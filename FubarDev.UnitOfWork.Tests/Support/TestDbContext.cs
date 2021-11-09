// <copyright file="TestDbContext.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using FubarDev.UnitOfWork.Tests.Support.DbModels;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FubarDev.UnitOfWork.Tests.Support
{
    public class TestDbContext : DbContext
    {
        public TestDbContext()
        {
        }

        [ActivatorUtilitiesConstructor]
        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<TestModel> TestModels { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite();
        }
    }
}
