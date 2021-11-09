// <copyright file="UnitOfWork.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.StatusManagement;

namespace FubarDev.UnitOfWork
{
    internal class UnitOfWork<TRepository> : IUnitOfWork<TRepository>
    {
        private readonly IRepositoryManager<TRepository> _repositoryManager;
        private readonly UnitOfWorkStatusItem<TRepository> _statusItem;
        private readonly UnitOfWorkFactory<TRepository> _factory;

        public UnitOfWork(
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
            return _factory.FinishAsync(_statusItem, StatusItemResult.Undefined);
        }
    }
}
