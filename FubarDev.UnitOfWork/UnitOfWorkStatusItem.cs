// <copyright file="UnitOfWorkStatusItem.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.StatusManagement;

using Nito.AsyncEx;

namespace FubarDev.UnitOfWork
{
    internal class UnitOfWorkStatusItem<TRepository> : IStatusItemInfo
    {
        private IUnitOfWork<TRepository>? _unitOfWork;

        public UnitOfWorkStatusItem(
            TRepository repository,
            bool isNewRepository,
            bool saveChangesOnDispose,
            Task initTask,
            IRepositoryTransaction? inheritedTransaction = null)
        {
            Repository = repository;
            IsNewRepository = isNewRepository;
            SaveChangesOnDispose = saveChangesOnDispose;
            InitTask = initTask;
            InheritedTransaction = inheritedTransaction;
        }

        public TRepository Repository { get; }

        public bool IsNewRepository { get; }

        /// <summary>
        /// Gets a value indicating whether the changes should be saved when the unit of work gets disposed.
        /// </summary>
        public bool SaveChangesOnDispose { get; }

        /// <summary>
        /// Gets the initialization task.
        /// </summary>
        public Task InitTask { get; }

        public IRepositoryTransaction? InheritedTransaction { get; set; }

        public IRepositoryTransaction? NewTransaction { get; set; }

        public bool OwnsTransaction => NewTransaction != null;

        /// <summary>
        /// Gets or sets the unit of work.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the UnitOfWork property wasn't initialized yet.</exception>
        public IUnitOfWork<TRepository> UnitOfWork
        {
            get => _unitOfWork ?? throw new InvalidOperationException("UnitOfWork not initialized yet");
            set => _unitOfWork = value;
        }

        /// <summary>
        /// Gets or sets the lazy task to get the transactional unit of work.
        /// </summary>
        public AsyncLazy<IRepositoryTransaction>? TransactionTask { get; set; }
    }
}
