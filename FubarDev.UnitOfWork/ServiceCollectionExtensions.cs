// <copyright file="ServiceCollectionExtensions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Diagnostics.Contracts;

using FubarDev.UnitOfWork;
using FubarDev.UnitOfWork.StatusManagement;

using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core services for units of work.
        /// </summary>
        /// <param name="services">The service collection this services should be added to.</param>
        /// <returns>The service collection the services were added to.</returns>
        public static IServiceCollection AddUnitOfWorkCore(
            this IServiceCollection services)
        {
            services.TryAdd(
                ServiceDescriptor.Singleton(typeof(IUnitOfWorkFactory<>), typeof(UnitOfWorkFactory<>)));
            services.TryAdd(
                ServiceDescriptor.Singleton(typeof(IStatusManager<>), typeof(DefaultStatusManager<>)));
            return services;
        }

        /// <summary>
        /// Adds the unit-of-work services.
        /// </summary>
        /// <param name="services">The service collection this services should be added to.</param>
        /// <typeparam name="TRepository">The repository type.</typeparam>
        /// <returns>The configuration interface the repository manager must be configured with.</returns>
        [Pure]
        public static IUnitOfWorkConfiguration<TRepository> AddUnitOfWork<TRepository>(
            this IServiceCollection services)
        {
            services.TryAdd(
                ServiceDescriptor.Singleton(typeof(IStatusManager<>), typeof(DefaultStatusManager<>)));
            services.TryAddSingleton<IUnitOfWorkFactory<TRepository>, UnitOfWorkFactory<TRepository>>();
            return new UnitOfWorkConfig<TRepository>(services);
        }

        /// <summary>
        /// Adds the unit-of-work services.
        /// </summary>
        /// <param name="services">The service collection this services should be added to.</param>
        /// <param name="repositoryManager">The repository manager instance.</param>
        /// <typeparam name="TRepository">The repository type.</typeparam>
        /// <returns>The service collection the services were added to.</returns>
        public static IServiceCollection AddUnitOfWork<TRepository>(
            this IServiceCollection services,
            IRepositoryManager<TRepository> repositoryManager)
        {
            services.TryAdd(
                ServiceDescriptor.Singleton(typeof(IStatusManager<>), typeof(DefaultStatusManager<>)));
            services.TryAddSingleton<IUnitOfWorkFactory<TRepository>, UnitOfWorkFactory<TRepository>>();
            return services.AddSingleton(repositoryManager);
        }

        private class UnitOfWorkConfig<TRepository> : IUnitOfWorkConfiguration<TRepository>
        {
            private readonly IServiceCollection _services;

            public UnitOfWorkConfig(IServiceCollection services)
            {
                _services = services;
            }

            public IServiceCollection Use<TRepositoryManager>()
                where TRepositoryManager : class, IRepositoryManager<TRepository>
            {
                return _services
                    .AddSingleton<IRepositoryManager<TRepository>, TRepositoryManager>();
            }
        }
    }
}
