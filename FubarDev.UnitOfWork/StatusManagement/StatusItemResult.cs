// <copyright file="StatusItemResult.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.UnitOfWork.StatusManagement
{
    /// <summary>
    /// The status of the units of work.
    /// </summary>
    public enum StatusItemResult
    {
        /// <summary>
        /// The status of the unit of work is indeterminate.
        /// </summary>
        Undefined,

        /// <summary>
        /// The changes should be committed.
        /// </summary>
        Commit,

        /// <summary>
        /// The changes should be rolled back.
        /// </summary>
        Rollback,
    }
}
