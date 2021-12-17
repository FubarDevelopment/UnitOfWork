// <copyright file="NonTransactionalTests.cs" company="Fubar Development Junker">
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
    public class NonTransactionalTests : IAsyncDisposable
    {
        private readonly CountingRepositoryManager _repositoryManager = new();
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory<CountingRepository> _factory;
        private readonly IUnitOfWorkFactoryStatus<CountingRepository> _factoryStatus;

        public NonTransactionalTests()
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
        public void TestNoOp()
        {
            Assert.Empty(_repositoryManager.Creations);
            Assert.Empty(_repositoryManager.SavedChanges);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
        }

        [Fact]
        public async Task TestDisposeAsync()
        {
            CountingRepository repositoryId;
            await using (var unitOfWork = await _factory.CreateAsync())
            {
                // Do nothing
                repositoryId = unitOfWork.Repository;
            }

            Assert.Contains(repositoryId, _repositoryManager.Creations);
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Equal(1, _repositoryManager.DisposedRepositories);
        }

        [Fact]
        public async Task TestNestedAsync()
        {
            CountingRepository repositoryId1, repositoryId2;
            await using (var unitOfWork1 = await _factory.CreateAsync())
            {
                repositoryId1 = unitOfWork1.Repository;
                await using (var unitOfWork2 = await _factory.CreateAsync())
                {
                    repositoryId2 = unitOfWork2.Repository;
                }
            }

            Assert.Equal(repositoryId1, repositoryId2);
            Assert.Equal(repositoryId1, Assert.Single(_repositoryManager.Creations));
            Assert.Equal(0, _repositoryManager.SavedChangesCount);
            Assert.Equal(1, _repositoryManager.DisposedRepositories);
        }
    }
}
