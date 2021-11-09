// <copyright file="TransactionalUnitOfWork.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.StatusManagement;

namespace FubarDev.UnitOfWork
{
    internal class TransactionalUnitOfWork<TRepository> : ITransactionalUnitOfWork<TRepository>
    {
        private readonly IRepositoryManager<TRepository> _repositoryManager;
        private readonly UnitOfWorkStatusItem<TRepository> _statusItem;
        private readonly UnitOfWorkFactory<TRepository> _factory;
        private StatusItemResult _status = StatusItemResult.Undefined;

        public TransactionalUnitOfWork(
            TRepository repository,
            IRepositoryManager<TRepository> repositoryManager,
            UnitOfWorkStatusItem<TRepository> statusItem,
            UnitOfWorkFactory<TRepository> factory)
        {
            _repositoryManager = repositoryManager;
            _statusItem = statusItem;
            _factory = factory;
            Repository = repository;
        }

        public string Id { get; } = UnitOfWorkIdGenerator.CreateId();

        public TRepository Repository { get; }

        public ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _repositoryManager.SaveChangesAsync(Repository, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _status == StatusItemResult.Undefined
                ? RollbackAsync()
                : default;
        }

        public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            return FinishAsync(StatusItemResult.Commit, cancellationToken);
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            return FinishAsync(StatusItemResult.Rollback, cancellationToken);
        }

        private async ValueTask FinishAsync(
            StatusItemResult status,
            CancellationToken cancellationToken = default)
        {
            await _factory.FinishAsync(_statusItem, status, cancellationToken);
            _status = status;
        }
    }
}
