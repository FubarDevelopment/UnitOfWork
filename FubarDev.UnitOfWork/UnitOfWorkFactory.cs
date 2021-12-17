// <copyright file="UnitOfWorkFactory.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.StatusManagement;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            _saveChangesOnDispose = options?.Value.SaveChangesWhenDisposingUnitOfWork ?? true;
            _repositoryManager = repositoryManager;
            _logger = logger;
            _statusManager = new DefaultStatusManager<UnitOfWorkStatusItem<TRepository>>(
                logger);
        }

        /// <inheritdoc />
        public IEnumerable<IUnitOfWork<TRepository>> ActiveUnitsOfWork =>
            _statusManager.ManagedStatusItems.Select(x => _unitOfWorkByStatusItems[x]);

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
            var repository = _statusManager.TryGetCurrent(out var currentStatusItem)
                ? currentStatusItem.Repository
                : _repositoryManager.Create();

            var newStatusItem = new UnitOfWorkStatusItem<TRepository>(
                repository,
                currentStatusItem == null,
                saveChangesOnDispose,
                currentStatusItem?.InheritedTransaction);
            _statusManager.Add(newStatusItem);

            var unitOfWork = new UnitOfWork<TRepository>(
                repository,
                _repositoryManager,
                newStatusItem,
                this);
            _unitOfWorkByStatusItems[newStatusItem] = unitOfWork;

            _logger?.LogInformation("Created unit of work {Id}", unitOfWork.Id);

            if (!newStatusItem.IsNewRepository)
            {
                return new ValueTask<IUnitOfWork<TRepository>>(unitOfWork);
            }

            var resultTask = Task.Run<IUnitOfWork<TRepository>>(
                async () =>
                {
                    await _repositoryManager.InitializeAsync(repository, cancellationToken);
                    return unitOfWork;
                },
                cancellationToken);
            return new ValueTask<IUnitOfWork<TRepository>>(resultTask);
        }

        /// <inheritdoc />
        public ValueTask<ITransactionalUnitOfWork<TRepository>> CreateTransactionalAsync(
            CancellationToken cancellationToken)
        {
            var repository = _statusManager.TryGetCurrent(out var currentStatusItem)
                ? currentStatusItem.Repository
                : _repositoryManager.Create();

            var wantsNewTransaction =
                _allowNestedTransactions
                || currentStatusItem?.InheritedTransaction == null;

            var newStatusItem = new UnitOfWorkStatusItem<TRepository>(
                repository,
                false,
                currentStatusItem == null);
            _statusManager.Add(newStatusItem);

            // Register the unit of work
            var unitOfWork = new TransactionalUnitOfWork<TRepository>(
                repository,
                _repositoryManager,
                newStatusItem,
                this);
            _unitOfWorkByStatusItems[newStatusItem] = unitOfWork;

            _logger?.LogInformation("Created unit of work {Id}", unitOfWork.Id);

            var resultTask = Task.FromResult<ITransactionalUnitOfWork<TRepository>>(unitOfWork);
            if (newStatusItem.IsNewRepository)
            {
                resultTask = resultTask.ContinueWith(
                        async t =>
                        {
                            await _repositoryManager.InitializeAsync(repository, cancellationToken);
                            return t.Result;
                        },
                        cancellationToken)
                    .Unwrap();
            }

            if (wantsNewTransaction)
            {
                // Workaround to ensure that the unit of work gets stored in the correct
                // AsyncLocal value.
                resultTask = resultTask.ContinueWith(
                        async t =>
                        {
                            // Ensure that the transaction is started
                            IRepositoryTransaction inheritedTransaction;
                            IRepositoryTransaction? newTransaction;
                            if (_allowNestedTransactions
                                || currentStatusItem?.InheritedTransaction == null)
                            {
                                newTransaction = inheritedTransaction =
                                    await _repositoryManager.StartTransactionAsync(repository, cancellationToken);
                            }
                            else
                            {
                                newTransaction = null;
                                inheritedTransaction = currentStatusItem.InheritedTransaction;
                            }

                            newStatusItem.InheritedTransaction = inheritedTransaction;
                            newStatusItem.NewTransaction = newTransaction;

                            return t.Result;
                        },
                        cancellationToken)
                    .Unwrap();
            }

            return new ValueTask<ITransactionalUnitOfWork<TRepository>>(resultTask);
        }

        internal async ValueTask FinishAsync(
            UnitOfWorkStatusItem<TRepository> statusItem,
            StatusItemResult status,
            CancellationToken cancellationToken = default)
        {
            var finalizingInfo = _statusManager.Complete(statusItem, status);
            foreach (var item in finalizingInfo.CompletedStatusItems)
            {
                var unitOfWork = _unitOfWorkByStatusItems[item];
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

                    if (transaction is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (transaction is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                if (item.IsNewRepository)
                {
                    var repository = item.Repository;
                    if (repository is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (repository is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
