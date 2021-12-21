// <copyright file="CountingRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace FubarDev.UnitOfWork.Tests.Support
{
    internal class CountingRepositoryManager : IRepositoryManager<CountingRepository>
    {
        private readonly Dictionary<CountingRepository, int> _commits = new();
        private readonly Dictionary<CountingRepository, int> _savedChanges = new();
        private readonly Dictionary<CountingRepository, int> _rollbacks = new();

        public HashSet<CountingRepository> Creations { get; } = new();

        public IReadOnlyDictionary<CountingRepository, int> SavedChanges => _savedChanges;

        public int SavedChangesCount => SavedChanges.Count == 0 ? 0 : SavedChanges.Values.Sum();

        public IReadOnlyDictionary<CountingRepository, int> Commits => _commits;

        public IReadOnlyDictionary<CountingRepository, int> Rollbacks => _rollbacks;

        public int CommitCount => Commits.Count == 0 ? 0 : Commits.Values.Sum();

        public int RollbackCount => Rollbacks.Count == 0 ? 0 : Rollbacks.Values.Sum();

        public int DisposedRepositories { get; private set; }

        public int DisposedTransactions { get; private set; }

        public void CountDisposedRepository(CountingRepository repository)
        {
            DisposedRepositories += 1;
        }

        public CountingRepository Create()
        {
            lock (this)
            {
                var result = new CountingRepository(this, Guid.NewGuid());
                Assert.True(Creations.Add(result));
                _savedChanges.Add(result, 0);
                Debug.WriteLine($"Created repository with ID {result.Id}");
                return result;
            }
        }

        public ValueTask InitializeAsync(
            CountingRepository repository,
            CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"Initialized repository with ID {repository.Id}");
            return default;
        }

        public ValueTask<IRepositoryTransaction> StartTransactionAsync(
            CountingRepository repository,
            CancellationToken cancellationToken)
        {
            lock (this)
            {
                Debug.WriteLine($"Try add repository with ID {repository.Id}");
                _rollbacks.Add(repository, 0);
                _commits.Add(repository, 0);
                var result = new RepositoryTransaction(this, repository);
                return ValueTask.FromResult<IRepositoryTransaction>(result);
            }
        }

        public ValueTask SaveChangesAsync(CountingRepository repository, CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                _savedChanges[repository] += 1;
                return ValueTask.CompletedTask;
            }
        }

        private class RepositoryTransaction : IRepositoryTransaction, IAsyncDisposable
        {
            private readonly CountingRepositoryManager _repositoryManager;
            private readonly CountingRepository _repositoryId;

            public RepositoryTransaction(
                CountingRepositoryManager repositoryManager,
                CountingRepository repositoryId)
            {
                _repositoryManager = repositoryManager;
                _repositoryId = repositoryId;
            }

            public ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                lock (_repositoryManager)
                {
                    _repositoryManager._commits[_repositoryId] += 1;
                    return ValueTask.CompletedTask;
                }
            }

            public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                lock (_repositoryManager)
                {
                    _repositoryManager._rollbacks[_repositoryId] += 1;
                    return ValueTask.CompletedTask;
                }
            }

            public ValueTask DisposeAsync()
            {
                lock (_repositoryManager)
                {
                    _repositoryManager.DisposedTransactions += 1;
                    return default;
                }
            }
        }
    }
}
