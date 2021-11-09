// <copyright file="IStatusFinalizingInfo.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Finalization information for status items.
    /// </summary>
    /// <typeparam name="TStatusItem">The type of the status item.</typeparam>
    public interface IStatusFinalizingInfo<TStatusItem>
        where TStatusItem : class, IStatusItemInfo
    {
        /// <summary>
        /// Gets a value indicating whether the manager has no active status items.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets the list of the status items that were completed.
        /// </summary>
        IReadOnlyCollection<TStatusItem> CompletedStatusItems { get; }

        /// <summary>
        /// Gets the effective status item result for a given status item.
        /// </summary>
        /// <param name="statusItem">The status item to get the result for.</param>
        /// <returns>The effective result for the given status item.</returns>
        StatusItemResult GetEffectiveResult(TStatusItem statusItem);

        /// <summary>
        /// Gets the provided status item result for a given status item.
        /// </summary>
        /// <param name="statusItem">The status item to get the result for.</param>
        /// <returns>The result for the given status item.</returns>
        StatusItemResult GetResult(TStatusItem statusItem);
    }
}
