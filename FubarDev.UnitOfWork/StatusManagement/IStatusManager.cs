// <copyright file="IStatusManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Interface for a status manager.
    /// </summary>
    /// <typeparam name="TStatusItem">The type of the status item.</typeparam>
    public interface IStatusManager<TStatusItem>
        where TStatusItem : class, IStatusItemInfo
    {
        /// <summary>
        /// Gets a value indicating whether the manager contains active status items.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets the managed status items (even the completed ones).
        /// </summary>
        IEnumerable<TStatusItem> ManagedStatusItems { get; }

        /// <summary>
        /// Gets the active (non-completed) status items.
        /// </summary>
        IEnumerable<TStatusItem> ActiveStatusItems { get; }

        /// <summary>
        /// Tries to get the currently active status item.
        /// </summary>
        /// <param name="statusItem">The active status item (if available).</param>
        /// <returns><see langword="true"/> if an active status item could be found.</returns>
        bool TryGetCurrent([NotNullWhen(true)] out TStatusItem? statusItem);

        /// <summary>
        /// Adds a new active status item.
        /// </summary>
        /// <param name="statusItem">The new active status item.</param>
        void Add(TStatusItem statusItem);

        /// <summary>
        /// Completes the given status item.
        /// </summary>
        /// <param name="statusItem">The completed status item.</param>
        /// <param name="result">The status item result.</param>
        /// <returns>The status items to be finalized.</returns>
        IStatusFinalizingInfo<TStatusItem> Complete(
            TStatusItem statusItem,
            StatusItemResult result);
    }
}
