// <copyright file="IRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// The manager for a repository of type <typeparamref name="TRepository"/>.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    public interface IRepositoryManager<TRepository>
    {
        /// <summary>
        /// Creates a new instance of the repository.
        /// </summary>
        /// <returns>The new repository.</returns>
        TRepository Create();

        /// <summary>
        /// Initializes the repository.
        /// </summary>
        /// <param name="repository">The repository to initialize.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The task.</returns>
        ValueTask InitializeAsync(
            TRepository repository,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a transaction for a given repository.
        /// </summary>
        /// <param name="repository">The repository to start the transaction for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The new repository transaction.</returns>
        ValueTask<IRepositoryTransaction> StartTransactionAsync(
            TRepository repository,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the changes for the given repository.
        /// </summary>
        /// <param name="repository">The repository to save the changes for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The task.</returns>
        ValueTask SaveChangesAsync(TRepository repository, CancellationToken cancellationToken = default);
    }
}
