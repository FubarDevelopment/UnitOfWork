// <copyright file="IRepositoryTransaction.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Transaction for a given repository.
    /// </summary>
    public interface IRepositoryTransaction
    {
        /// <summary>
        /// Writes all changes to the disk.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The task.</returns>
        ValueTask CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reverts all changes made through the repository.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The task.</returns>
        ValueTask RollbackAsync(CancellationToken cancellationToken = default);
    }
}
