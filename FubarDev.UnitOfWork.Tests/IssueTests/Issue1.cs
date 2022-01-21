// <copyright file="Issue1.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace FubarDev.UnitOfWork.Tests.IssueTests
{
    public class Issue1 : IAsyncDisposable
    {
        private readonly FailingRepositoryManager _repositoryManager = new();
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory<DummyRepository> _factory;
        private readonly IUnitOfWorkFactoryStatus<DummyRepository> _factoryStatus;

        public Issue1()
        {
            var services = new ServiceCollection()
                .AddUnitOfWork(_repositoryManager);
            _serviceProvider = services.BuildServiceProvider(true);
            _factory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory<DummyRepository>>();
            _factoryStatus = (IUnitOfWorkFactoryStatus<DummyRepository>)_factory;
        }

        public async ValueTask DisposeAsync()
        {
            if (_factoryStatus != null)
            {
                Assert.Empty(_factoryStatus.ActiveUnitsOfWork);
            }

            await _serviceProvider.DisposeAsync();
        }

        [Fact(DisplayName = "Issue 1 - A failing CommitAsync causes two session removals")]
        public async Task Test()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await using var unitOfWork = await _factory.CreateTransactionalAsync();
                    await unitOfWork.CommitAsync();
                });
            Assert.Equal("me bad!", exception.Message);
        }

        private class DummyRepository
        {
        }

        private class FailingRepositoryManager : IRepositoryManager<DummyRepository>
        {
            public DummyRepository Create()
            {
                return new DummyRepository();
            }

            public ValueTask InitializeAsync(DummyRepository repository, CancellationToken cancellationToken = default)
            {
                return default;
            }

            public ValueTask<IRepositoryTransaction> StartTransactionAsync(DummyRepository repository, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<IRepositoryTransaction>(new FailingTransaction());
            }

            public ValueTask SaveChangesAsync(DummyRepository repository, CancellationToken cancellationToken = default)
            {
                return default;
            }

            private class FailingTransaction : IRepositoryTransaction
            {
                public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
                {
                    await Task.Delay(0, cancellationToken);
                    throw new InvalidOperationException("me bad!");
                }

                public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
                {
                    return default;
                }
            }
        }
    }
}
