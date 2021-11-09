// <copyright file="NhRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

using NHibernate;

namespace FubarDev.UnitOfWork.NhSession
{
    /// <summary>
    /// Implementation for <see cref="IRepositoryManager{TRepository}"/> with NHibernate
    /// <see cref="ISession"/> as repository and an <see cref="ISessionFactory"/> as required
    /// service.
    /// </summary>
    public class NhRepositoryManager : IRepositoryManager<ISession>
    {
        private readonly ISessionFactory _sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NhRepositoryManager"/> class.
        /// </summary>
        /// <param name="sessionFactory">The session factory to create the session with.</param>
        public NhRepositoryManager(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <summary>
        /// Gets the session factory.
        /// </summary>
        protected ISessionFactory SessionFactory => _sessionFactory;

        /// <inheritdoc />
        public virtual ISession Create()
        {
            return _sessionFactory.OpenSession();
        }

        /// <inheritdoc />
        public virtual ValueTask InitializeAsync(ISession repository, CancellationToken cancellationToken = default)
        {
            return default;
        }

        /// <inheritdoc />
        public ValueTask<IRepositoryTransaction> StartTransactionAsync(
            ISession repository,
            CancellationToken cancellationToken = default)
        {
            var transaction = repository.BeginTransaction();
            var result = new NhTransaction(transaction);
            return new ValueTask<IRepositoryTransaction>(result);
        }

        /// <inheritdoc />
        public async ValueTask SaveChangesAsync(ISession repository, CancellationToken cancellationToken = default)
        {
            await repository.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Implementation for <see cref="IRepositoryTransaction"/> with NHibernate transactions.
        /// </summary>
        private class NhTransaction : IRepositoryTransaction, IDisposable
        {
            private readonly ITransaction _transaction;

            /// <summary>
            /// Initializes a new instance of the <see cref="NhTransaction"/> class.
            /// </summary>
            /// <param name="transaction">The NHibernate transaction.</param>
            public NhTransaction(ITransaction transaction)
            {
                _transaction = transaction;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _transaction.Dispose();
            }

            /// <inheritdoc />
            public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            /// <inheritdoc />
            public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
        }
    }
}
