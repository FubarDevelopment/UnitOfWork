// <copyright file="UnitOfWorkStatusItem.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using FubarDev.UnitOfWork.StatusManagement;

namespace FubarDev.UnitOfWork
{
    internal class UnitOfWorkStatusItem<TRepository> : IStatusItemInfo
    {
        public UnitOfWorkStatusItem(
            TRepository repository,
            bool isNewRepository,
            bool saveChangesOnDispose,
            IRepositoryTransaction? inheritedTransaction = null)
        {
            Repository = repository;
            IsNewRepository = isNewRepository;
            SaveChangesOnDispose = saveChangesOnDispose;
            InheritedTransaction = inheritedTransaction;
        }

        public TRepository Repository { get; }

        public bool IsNewRepository { get; }

        /// <summary>
        /// Gets a value indicating whether the changes should be saved when the unit of work gets disposed.
        /// </summary>
        public bool SaveChangesOnDispose { get; }

        public IRepositoryTransaction? InheritedTransaction { get; set; }

        public IRepositoryTransaction? NewTransaction { get; set; }

        public bool OwnsTransaction => NewTransaction != null;
    }
}
