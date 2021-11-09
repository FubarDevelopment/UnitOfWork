// <copyright file="IStatusItemInfo.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// Information about a status item.
    /// </summary>
    public interface IStatusItemInfo
    {
        /// <summary>
        /// Gets a value indicating whether the status item owns the transaction.
        /// </summary>
        bool OwnsTransaction { get; }
    }
}
