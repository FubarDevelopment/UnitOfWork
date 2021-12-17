// <copyright file="UnitOfWorkFactoryOptions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// The options for the <see cref="UnitOfWorkFactory{TRepository}"/>.
    /// </summary>
    public class UnitOfWorkFactoryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether nested transactions are allowed.
        /// </summary>
        public bool AllowNestedTransactions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the changes should be automatically saved when a
        /// unit of work gets disposed.
        /// </summary>
        public bool SaveChangesWhenDisposingUnitOfWork { get; set; }
    }
}
