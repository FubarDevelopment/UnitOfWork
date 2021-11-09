// <copyright file="EfCoreRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FubarDev.UnitOfWork.EfCore
{
    /// <summary>
    /// Implementation of <see cref="IRepositoryManager{TRepository}"/> for EF Core.
    /// </summary>
    /// <typeparam name="TDbContext">The DB context type.</typeparam>
    public class EfCoreRepositoryManager<TDbContext> : IRepositoryManager<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IDbContextFactory<TDbContext> _contextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfCoreRepositoryManager{TDbContext}"/> class.
        /// </summary>
        /// <param name="contextFactory">The factory for the DB context.</param>
        public EfCoreRepositoryManager(IDbContextFactory<TDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <inheritdoc />
        public TDbContext Create()
        {
            return _contextFactory.CreateDbContext();
        }

        /// <inheritdoc />
        public virtual ValueTask InitializeAsync(TDbContext repository, CancellationToken cancellationToken = default)
        {
            return default;
        }

        /// <inheritdoc />
        public async ValueTask<IRepositoryTransaction> StartTransactionAsync(TDbContext repository, CancellationToken cancellationToken = default)
        {
            var transaction = await repository.Database.BeginTransactionAsync(cancellationToken);
            return new EfTransaction(transaction);
        }

        /// <inheritdoc />
        public async ValueTask SaveChangesAsync(TDbContext repository, CancellationToken cancellationToken = default)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        private class EfTransaction : IRepositoryTransaction, IAsyncDisposable
        {
            private readonly IDbContextTransaction _transaction;

            public EfTransaction(IDbContextTransaction transaction)
            {
                _transaction = transaction;
            }

            public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }

            public ValueTask DisposeAsync()
            {
                return _transaction.DisposeAsync();
            }
        }
    }
}
