// <copyright file="IUnitOfWorkFactoryStatus.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Interface to query the status of the unit of work factory.
    /// </summary>
    /// <typeparam name="TRepository">The repository type.</typeparam>
    public interface IUnitOfWorkFactoryStatus<out TRepository>
    {
        /// <summary>
        /// Gets the active unit of work instances.
        /// </summary>
        IEnumerable<IUnitOfWork<TRepository>> ActiveUnitsOfWork { get; }
    }
}
