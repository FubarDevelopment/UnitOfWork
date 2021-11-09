// <copyright file="ITransactionalUnitOfWork.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// A transactional unit of work with access to a given repository.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository which can be accessed through a unit o
    /// work.</typeparam>
    public interface ITransactionalUnitOfWork<out TRepository>
        : IUnitOfWork<TRepository>
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
