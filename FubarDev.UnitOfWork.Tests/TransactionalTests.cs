// <copyright file="TransactionalTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace FubarDev.UnitOfWork.Tests
{
    public class TransactionalTests : IAsyncDisposable
    {
        private readonly CountingRepositoryManager _repositoryManager = new();
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory<CountingRepository> _factory;
        private readonly IUnitOfWorkFactoryStatus<CountingRepository> _factoryStatus;

        public TransactionalTests()
        {
            var services = new ServiceCollection()
                .AddUnitOfWork(_repositoryManager);
            _serviceProvider = services.BuildServiceProvider(true);
            _factory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory<CountingRepository>>();
            _factoryStatus = (IUnitOfWorkFactoryStatus<CountingRepository>)_factory;
        }

        public async ValueTask DisposeAsync()
        {
            if (_factoryStatus != null)
            {
                Assert.Empty(_factoryStatus.ActiveUnitsOfWork);
            }

            await _serviceProvider.DisposeAsync();
        }

        [Fact]
        public async Task TestDisposeAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateTransactionalAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;

                var activeUnitOfWork = Assert.Single(_factoryStatus.ActiveUnitsOfWork);
                Assert.Equal(repositoryId, activeUnitOfWork.Repository);
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId, _repositoryManager.Commits);
            Assert.Contains(repositoryId, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
            Assert.Equal(1, _repositoryManager.DisposedRepositories);
            Assert.Equal(1, _repositoryManager.DisposedTransactions);
        }

        [Fact]
        public async Task TestRollbackAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateTransactionalAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;
                await unitOfWork.RollbackAsync();

                Assert.Empty(_factoryStatus.ActiveUnitsOfWork);
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId, _repositoryManager.Commits);
            Assert.Contains(repositoryId, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestCommitAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateTransactionalAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;
                await unitOfWork.CommitAsync();
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId, _repositoryManager.Commits);
            Assert.Contains(repositoryId, _repositoryManager.Rollbacks);
            Assert.Equal(1, _repositoryManager.CommitCount);
            Assert.Equal(0, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestDoubleFinishAfterCommitFailsAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateTransactionalAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;
                await unitOfWork.CommitAsync();
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await unitOfWork.CommitAsync());
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await unitOfWork.RollbackAsync());
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId, _repositoryManager.Commits);
            Assert.Contains(repositoryId, _repositoryManager.Rollbacks);
            Assert.Equal(1, _repositoryManager.CommitCount);
            Assert.Equal(0, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestDoubleFinishAfterRollbackFailsAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateTransactionalAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;
                await unitOfWork.RollbackAsync();
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await unitOfWork.CommitAsync());
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await unitOfWork.RollbackAsync());
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId, _repositoryManager.Commits);
            Assert.Contains(repositoryId, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedWithNonTransactionalAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                }

                await unitOfWork1.RollbackAsync();
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedWithNonTransactionalInWrongOrderAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateAsync())
                {
                    await unitOfWork1.RollbackAsync();

                    repositoryId2 = unitOfWork2.Repository;

                    Assert.Equal(2, _factoryStatus.ActiveUnitsOfWork.Count());
                }

                Assert.Empty(_factoryStatus.ActiveUnitsOfWork);
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedDisposeInCommitAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                }

                await unitOfWork1.CommitAsync();
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedRollbackInCommitAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                    await unitOfWork2.RollbackAsync();
                }

                await unitOfWork1.CommitAsync();
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedCommitInCommitAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                    await unitOfWork2.CommitAsync();
                }

                await unitOfWork1.CommitAsync();
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(1, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(1, _repositoryManager.CommitCount);
            Assert.Equal(0, _repositoryManager.RollbackCount);
        }

        [Fact]
        public async Task TestNestedCommitInRollbackAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                repositoryId1 = unitOfWork1.Repository;

                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                    await unitOfWork2.CommitAsync();
                }

                await unitOfWork1.RollbackAsync();
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(1, _repositoryManager.SavedChangesCount);
            Assert.Contains(repositoryId1, _repositoryManager.Commits);
            Assert.Contains(repositoryId1, _repositoryManager.Rollbacks);
            Assert.Equal(0, _repositoryManager.CommitCount);
            Assert.Equal(1, _repositoryManager.RollbackCount);
            Assert.Equal(1, _repositoryManager.DisposedRepositories);
            Assert.Equal(1, _repositoryManager.DisposedTransactions);
        }

        [Fact]
        public async Task TestConcurrencyAsync()
        {
            var taskCount = Environment.ProcessorCount * 10;

            var tasks = Enumerable.Range(0, taskCount)
                .Select(_ => _factory.CreateTransactionalAsync().AsTask())
                .ToList();

            var unitsOfWork = await Task.WhenAll(tasks);

            Assert.Equal(taskCount, _factoryStatus.ActiveUnitsOfWork.Count());

            var disposeTasks = unitsOfWork.Select(static x => x.DisposeAsync().AsTask()).ToList();
            await Task.WhenAll(disposeTasks);

            Assert.Empty(_factoryStatus.ActiveUnitsOfWork);

            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Equal(1, _repositoryManager.DisposedTransactions);
            Assert.Equal(1, _repositoryManager.RollbackCount);
            Assert.Equal(1, _repositoryManager.DisposedRepositories);
        }
    }
}
