// <copyright file="EfCoreTestRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.EfCore;

using Microsoft.EntityFrameworkCore;

namespace FubarDev.UnitOfWork.Tests.Support
{
    public class EfCoreTestRepositoryManager : EfCoreRepositoryManager<TestDbContext>
    {
        public EfCoreTestRepositoryManager(
            IDbContextFactory<TestDbContext> contextFactory)
            : base(contextFactory)
        {
        }

        public override async ValueTask InitializeAsync(TestDbContext repository, CancellationToken cancellationToken = default)
        {
            await repository.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
