// <copyright file="IUnitOfWorkFactory.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Factory for the creation of unit of works.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository to be accessible through the unit of work.</typeparam>
    public interface IUnitOfWorkFactory<TRepository>
    {
        /// <summary>
        /// Creates a new unit of work.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The new unit of work.</returns>
        ValueTask<IUnitOfWork<TRepository>> CreateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new transactional unit of work.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The new unit of work.</returns>
        ValueTask<ITransactionalUnitOfWork<TRepository>> CreateTransactionalAsync(
            CancellationToken cancellationToken = default);
    }
}
