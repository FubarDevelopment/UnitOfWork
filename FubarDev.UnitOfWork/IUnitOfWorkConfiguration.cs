// <copyright file="IUnitOfWorkConfiguration.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace FubarDev.UnitOfWork
{
    /// <summary>
    /// Custom interface for the configuration of the <see cref="UnitOfWorkFactory{TRepository}"/>.
    /// </summary>
    /// <typeparam name="TRepository">The repository type.</typeparam>
    public interface IUnitOfWorkConfiguration<TRepository>
    {
        /// <summary>
        /// Defines the repository manager service type to be used as singleton.
        /// </summary>
        /// <typeparam name="TRepositoryManager">The repository manager type.</typeparam>
        /// <returns>The service collection this repository manager was added to.</returns>
        IServiceCollection Use<TRepositoryManager>()
            where TRepositoryManager : class, IRepositoryManager<TRepository>;
    }
}
