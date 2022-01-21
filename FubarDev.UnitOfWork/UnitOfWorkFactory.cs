// <copyright file="UnitOfWorkFactory.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.StatusManagement;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nito.AsyncEx;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Default implementation of <see cref="IUnitOfWorkFactory{TRepository}"/>.
    /// </summary>
    /// <typeparam name="TRepository">The repository type.</typeparam>
    public class UnitOfWorkFactory<TRepository>
        : IUnitOfWorkFactory<TRepository>,
            IUnitOfWorkFactoryStatus<TRepository>
    {
        private readonly object _statusManagerLock = new();
        private readonly IStatusManager<UnitOfWorkStatusItem<TRepository>> _statusManager;
        private readonly IRepositoryManager<TRepository> _repositoryManager;
        private readonly ILogger<UnitOfWorkFactory<TRepository>>? _logger;
        private readonly bool _allowNestedTransactions;
        private readonly bool _saveChangesOnDispose;

        private readonly Dictionary<UnitOfWorkStatusItem<TRepository>, IUnitOfWork<TRepository>>
            _unitOfWorkByStatusItems = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkFactory{TRepository}"/> class.
        /// </summary>
        /// <param name="repositoryManager">The repository manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options.</param>
        public UnitOfWorkFactory(
            IRepositoryManager<TRepository> repositoryManager,
            ILogger<UnitOfWorkFactory<TRepository>>? logger = null,
            IOptions<UnitOfWorkFactoryOptions>? options = null)
        {
            _allowNestedTransactions = options?.Value.AllowNestedTransactions ?? false;
            _saveChangesOnDispose = options?.Value.SaveChangesWhenDisposingUnitOfWork ?? false;
            _repositoryManager = repositoryManager;
            _logger = logger;
            _statusManager = new DefaultStatusManager<UnitOfWorkStatusItem<TRepository>>(
                logger);
        }

        /// <inheritdoc />
        public IEnumerable<IUnitOfWork<TRepository>> ActiveUnitsOfWork
        {
            get
            {
                lock (_statusManagerLock)
                {
                    return _statusManager.ManagedStatusItems.Select(x => _unitOfWorkByStatusItems[x]).ToList();
                }
            }
        }

        /// <inheritdoc />
        public ValueTask<IUnitOfWork<TRepository>> CreateAsync(
            CancellationToken cancellationToken = default)
        {
            return CreateAsync(_saveChangesOnDispose, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask<IUnitOfWork<TRepository>> CreateAsync(
            bool saveChangesOnDispose,
            CancellationToken cancellationToken = default)
        {
            UnitOfWorkStatusItem<TRepository> newStatusItem;
            lock (_statusManagerLock)
            {
                var repository = _statusManager.TryGetCurrent(out var currentStatusItem)
                    ? currentStatusItem.Repository
                    : _repositoryManager.Create();

                var initTask = currentStatusItem == null
                    ? _repositoryManager.InitializeAsync(repository, cancellationToken).AsTask()
                    : currentStatusItem.InitTask;

                var isNewRepository = currentStatusItem == null;
                newStatusItem = new UnitOfWorkStatusItem<TRepository>(
                    repository,
                    isNewRepository,
                    saveChangesOnDispose,
                    initTask,
                    currentStatusItem?.InheritedTransaction);

                _statusManager.Add(newStatusItem);

                var unitOfWork = new UnitOfWork<TRepository>(
                    repository,
                    _repositoryManager,
                    newStatusItem,
                    this);
                _unitOfWorkByStatusItems[newStatusItem] = unitOfWork;

                newStatusItem.UnitOfWork = unitOfWork;

                _logger?.LogInformation("Created unit of work {Id}", unitOfWork.Id);
            }

            var resultTask = Task.FromResult(newStatusItem)
                .ContinueWith(
                    async t =>
                    {
                        await t.Result.InitTask;
                        return t.Result.UnitOfWork;
                    },
                    cancellationToken)
                .Unwrap();

            return new ValueTask<IUnitOfWork<TRepository>>(resultTask);
        }

        /// <inheritdoc />
        public ValueTask<ITransactionalUnitOfWork<TRepository>> CreateTransactionalAsync(
            CancellationToken cancellationToken)
        {
            UnitOfWorkStatusItem<TRepository> newStatusItem;
            lock (_statusManagerLock)
            {
                var repository = _statusManager.TryGetCurrent(out var currentStatusItem)
                    ? currentStatusItem.Repository
                    : _repositoryManager.Create();

                var initTask = currentStatusItem == null
                    ? _repositoryManager.InitializeAsync(repository, cancellationToken).AsTask()
                    : currentStatusItem.InitTask;

                var isNewRepository = currentStatusItem == null;
                newStatusItem = new UnitOfWorkStatusItem<TRepository>(
                    repository,
                    isNewRepository,
                    !isNewRepository,
                    initTask);

                _statusManager.Add(newStatusItem);

                // Register the unit of work
                var unitOfWork = new TransactionalUnitOfWork<TRepository>(
                    repository,
                    _repositoryManager,
                    newStatusItem,
                    this);
                _unitOfWorkByStatusItems[newStatusItem] = unitOfWork;

                newStatusItem.UnitOfWork = unitOfWork;
                newStatusItem.TransactionTask = new AsyncLazy<IRepositoryTransaction>(
                    async () => await InitTransaction(currentStatusItem, newStatusItem, cancellationToken));

                _logger?.LogInformation("Created transactional unit of work {Id}", unitOfWork.Id);
            }

            var resultTask = Task.FromResult(newStatusItem)
                .ContinueWith(
                    async t =>
                    {
                        await t.Result.InitTask;
                        _ = await t.Result.TransactionTask!;
                        return (ITransactionalUnitOfWork<TRepository>)t.Result.UnitOfWork;
                    },
                    cancellationToken)
                .Unwrap();

            return new ValueTask<ITransactionalUnitOfWork<TRepository>>(resultTask);
        }

        internal async ValueTask FinishAsync(
            UnitOfWorkStatusItem<TRepository> statusItem,
            StatusItemResult status,
            CancellationToken cancellationToken = default)
        {
            IStatusFinalizingInfo<UnitOfWorkStatusItem<TRepository>> finalizingInfo;
            lock (_statusManagerLock)
            {
                finalizingInfo = _statusManager.Complete(statusItem, status);
            }

            foreach (var item in finalizingInfo.CompletedStatusItems)
            {
                IUnitOfWork<TRepository> unitOfWork;
                lock (_statusManagerLock)
                {
                    unitOfWork = _unitOfWorkByStatusItems[item];
                    _unitOfWorkByStatusItems.Remove(item);
                }

                var finalStatus = finalizingInfo.GetEffectiveResult(item);
                _logger?.Log(
                    finalStatus.GetLogLevel(),
                    "Finalizing unit of work {Id} with effective status {Status}",
                    unitOfWork.Id,
                    finalStatus);

                if (statusItem.SaveChangesOnDispose
                    && finalStatus is StatusItemResult.Undefined or StatusItemResult.Commit)
                {
                    // Flush all changes
                    await _repositoryManager.SaveChangesAsync(item.Repository, cancellationToken);
                }

                var transaction = item.NewTransaction;
                if (transaction != null)
                {
                    // Committing the transaction should implicitly save the changes,
                    // while rolling back shouldn't.
                    switch (finalStatus)
                    {
                        case StatusItemResult.Commit:
                            await transaction.CommitAsync(cancellationToken);
                            break;
                        case StatusItemResult.Undefined:
                        case StatusItemResult.Rollback:
                            await transaction.RollbackAsync(cancellationToken);
                            break;
                        default:
                            throw new NotSupportedException(
                                $"Status {status} is nt supported for a transactional unit of work");
                    }

                    await TryDisposeAsync(transaction);
                }

                if (item.IsNewRepository)
                {
                    await TryDisposeAsync(item.Repository);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask TryDisposeAsync<T>(T item)
        {
            if (item is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private async Task<IRepositoryTransaction> InitTransaction(
            UnitOfWorkStatusItem<TRepository>? currentStatusItem,
            UnitOfWorkStatusItem<TRepository> newStatusItem,
            CancellationToken cancellationToken)
        {
            // Ensure that the transaction is started
            IRepositoryTransaction inheritedTransaction;
            IRepositoryTransaction? newTransaction;
            if (_allowNestedTransactions
                || currentStatusItem?.TransactionTask == null)
            {
                newTransaction = inheritedTransaction = await _repositoryManager.StartTransactionAsync(
                    newStatusItem.Repository,
                    cancellationToken);
            }
            else
            {
                inheritedTransaction = await currentStatusItem.TransactionTask;
                newTransaction = null;
            }

            newStatusItem.InheritedTransaction = inheritedTransaction;
            newStatusItem.NewTransaction = newTransaction;

            return newTransaction ?? inheritedTransaction;
        }
    }
}
