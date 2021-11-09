// <copyright file="DefaultStatusManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Default implementation of <see cref="IStatusManager{TStatusItem}"/>.
    /// </summary>
    /// <typeparam name="TStatusItem">The status item type.</typeparam>
    public class DefaultStatusManager<TStatusItem> : IStatusManager<TStatusItem>
        where TStatusItem : class, IStatusItemInfo
    {
        private readonly ILogger? _logger;
        private readonly object _lock = new();
        private readonly AsyncLocal<StatusItemsHolder?> _statusHolder = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultStatusManager{TStatusItem}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DefaultStatusManager(ILogger? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _statusHolder.Value == null
                           || _statusHolder.Value.Items.Count == 0;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TStatusItem> ManagedStatusItems
        {
            get
            {
                lock (_lock)
                {
                    return _statusHolder.Value?.Items.Select(x => x.StatusItem).ToList()
                           ?? Enumerable.Empty<TStatusItem>();
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TStatusItem> ActiveStatusItems
        {
            get
            {
                lock (_lock)
                {
                    return _statusHolder.Value?.Items.Where(x => !x.IsCompleted).Select(x => x.StatusItem).ToList()
                           ?? Enumerable.Empty<TStatusItem>();
                }
            }
        }

        /// <inheritdoc />
        public bool TryGetCurrent([NotNullWhen(true)] out TStatusItem? statusItem)
        {
            lock (_lock)
            {
                var statusHolder = _statusHolder.Value;
                if (statusHolder == null || statusHolder.IsEmpty)
                {
                    statusItem = default;
                    return false;
                }

                statusItem = statusHolder.Items.Peek().StatusItem;
                return true;
            }
        }

        /// <inheritdoc />
        public void Add(TStatusItem statusItem)
        {
            lock (_lock)
            {
                var statusHolder = _statusHolder.Value;
                if (statusHolder == null)
                {
                    _statusHolder.Value = statusHolder = new StatusItemsHolder();
                }

                var container = new StatusItemContainer<TStatusItem>(statusItem);
                statusHolder.Add(container);
                _logger?.LogInformation("Added status item {Id}", container.Id);
            }
        }

        /// <inheritdoc />
        public IStatusFinalizingInfo<TStatusItem> Complete(TStatusItem statusItem, StatusItemResult result)
        {
            var completedStatusItems = new List<StatusItemContainer<TStatusItem>>();
            lock (_lock)
            {
                var statusHolder = _statusHolder.Value;
                if (statusHolder == null)
                {
                    _logger?.LogError("Unit of work status not found (invalid async chain?)");
                    throw new InvalidOperationException(
                        "Failed to find the current UnitOfWork status - invalid async chain?");
                }

                if (!statusHolder.ItemContainers.TryGetValue(statusItem, out var container))
                {
                    // The unit of work couldn't be found
                    _logger?.LogError("Unit of work to complete not found (completed twice)");
                    throw new InvalidOperationException(
                        "The unit of work wasn't found - commit/rollback/dispose twice?");
                }

                // Set the items status
                container.Result = result;
                _logger?.LogInformation(
                    "Container {Id} set to status {Status}",
                    container.Id,
                    container.Result);

                if (!statusHolder.Remove(container))
                {
                    // Finished unit is not the last one...
                    _logger?.LogWarning(
                        "Container {Id} completed, but children are still active",
                        container.Id);
                    return new StatusFinalizingInfo<TStatusItem>(
                        statusHolder.IsEmpty,
                        completedStatusItems);
                }

                // Merge the new status
                container.EffectiveResult = StatusResultEvaluator.GetEffectiveResult(
                    statusHolder.Status,
                    container.Result);
                _logger?.LogWarning(
                    "Container {Id} completed with effective status {EffectiveStatus}",
                    container.Id,
                    container.EffectiveResult);
                statusHolder.Status = StatusResultEvaluator.ApplyResult(
                    statusHolder.Status,
                    statusItem,
                    container.Result);
                _logger?.LogWarning("Chain status changed to {Status}", statusHolder.Status);

                // Remember the status item
                completedStatusItems.Add(container);

                // Current unit was the last - try to remove
                // all items from the stack that are already
                // finished.
                while (statusHolder.TryRemoveFinished(out var next))
                {
                    _logger?.LogWarning("Container {Id} completed", next.Id);

                    // Merge the new status
                    next.EffectiveResult = StatusResultEvaluator.GetEffectiveResult(
                        statusHolder.Status,
                        next.Result);
                    _logger?.LogWarning(
                        "Container {Id} completed with effective status {EffectiveStatus}",
                        next.Id,
                        next.EffectiveResult);
                    statusHolder.Status = StatusResultEvaluator.ApplyResult(
                        statusHolder.Status,
                        next.StatusItem,
                        next.Result);
                    _logger?.LogWarning("Chain status changed to {Status}", statusHolder.Status);

                    // Remember the status item
                    completedStatusItems.Add(next);
                }

                var isEmpty = statusHolder.IsEmpty;

                // Reset the status if there are no active units of work.
                if (isEmpty)
                {
                    statusHolder.Reset();
                }

                return new StatusFinalizingInfo<TStatusItem>(
                    isEmpty,
                    completedStatusItems);
            }
        }

        private class StatusItemsHolder
        {
            /// <summary>
            /// Gets a value indicating whether this holder contains no status items.
            /// </summary>
            public bool IsEmpty => Items.Count == 0;

            /// <summary>
            /// Gets all active status item containers.
            /// </summary>
            public Stack<StatusItemContainer<TStatusItem>> Items { get; } = new();

            /// <summary>
            /// Gets the mapping from status item to its container.
            /// </summary>
            public Dictionary<TStatusItem, StatusItemContainer<TStatusItem>> ItemContainers { get; } = new();

            /// <summary>
            /// Gets or sets the final status.
            /// </summary>
            public StatusItemResult Status { get; set; } = StatusItemResult.Undefined;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                Status = StatusItemResult.Undefined;
                Items.Clear();
                ItemContainers.Clear();
            }

            /// <summary>
            /// Adds a new status item container.
            /// </summary>
            /// <param name="container">The container to be added.</param>
            public void Add(StatusItemContainer<TStatusItem> container)
            {
                Items.Push(container);
                ItemContainers[container.StatusItem] = container;
            }

            /// <summary>
            /// Try to remove a completed status item.
            /// </summary>
            /// <param name="item">The found (completed) item.</param>
            /// <returns><see langword="true"/>, when a completed status item could be found.</returns>
            public bool TryRemoveFinished([NotNullWhen(true)] out StatusItemContainer<TStatusItem>? item)
            {
                if (Items.Count == 0)
                {
                    item = null;
                    return false;
                }

                var next = Items.Pop();
                if (!next.IsCompleted)
                {
                    Items.Push(next);
                    item = null;
                    return false;
                }

                ItemContainers.Remove(next.StatusItem);

                item = next;
                return true;
            }

            /// <summary>
            /// Remove a status item container.
            /// </summary>
            /// <param name="item">The item container to be removed.</param>
            /// <returns><see langword="true"/> when the container could be removed.</returns>
            public bool Remove(StatusItemContainer<TStatusItem> item)
            {
                var last = Items.Pop();
                if (last != item)
                {
                    // Finished unit is not the last one...
                    Items.Push(last);
                    return false;
                }

                ItemContainers.Remove(item.StatusItem);
                return true;
            }
        }
    }
}
