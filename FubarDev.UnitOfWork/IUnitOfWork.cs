// <copyright file="IUnitOfWork.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// A unit of work with access to a given repository.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository which can be accessed through a unit o
    /// work.</typeparam>
    public interface IUnitOfWork<out TRepository> : IAsyncDisposable
    {
        /// <summary>
        /// Gets the ID of this unit of work.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the unit of work.
        /// </summary>
        TRepository Repository { get; }

        /// <summary>
        /// Writes all changes to the disk.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The task.</returns>
        ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
