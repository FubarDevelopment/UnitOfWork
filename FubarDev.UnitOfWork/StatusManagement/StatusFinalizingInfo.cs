// <copyright file="StatusFinalizingInfo.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Gets the information for the finalization of the completed status items.
    /// </summary>
    /// <typeparam name="TStatusItem">The status item type.</typeparam>
    internal class StatusFinalizingInfo<TStatusItem> : IStatusFinalizingInfo<TStatusItem>
        where TStatusItem : class, IStatusItemInfo
    {
        private readonly IReadOnlyDictionary<TStatusItem, StatusItemContainer<TStatusItem>> _completedStatusItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusFinalizingInfo{TStatusItem}"/> class.
        /// </summary>
        /// <param name="isEmpty">Indicates whether the manager has no more active items.</param>
        /// <param name="completedStatusItems">The completed status items.</param>
        public StatusFinalizingInfo(
            bool isEmpty,
            IReadOnlyCollection<StatusItemContainer<TStatusItem>> completedStatusItems)
        {
            _completedStatusItems = completedStatusItems.ToDictionary(x => x.StatusItem);
            CompletedStatusItems = completedStatusItems.Select(x => x.StatusItem).ToList();
            IsEmpty = isEmpty;
        }

        /// <inheritdoc />
        public bool IsEmpty { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<TStatusItem> CompletedStatusItems { get; }

        /// <inheritdoc />
        public StatusItemResult GetEffectiveResult(TStatusItem statusItem)
        {
            if (!_completedStatusItems.TryGetValue(statusItem, out var container))
            {
                throw new InvalidOperationException("The status item could not be found");
            }

            return container.EffectiveResult
                   ?? throw new InvalidOperationException("The status item is missing the effective result");
        }

        /// <inheritdoc />
        public StatusItemResult GetResult(TStatusItem statusItem)
        {
            if (!_completedStatusItems.TryGetValue(statusItem, out var container))
            {
                throw new InvalidOperationException("The status item could not be found");
            }

            return container.Result;
        }
    }
}
