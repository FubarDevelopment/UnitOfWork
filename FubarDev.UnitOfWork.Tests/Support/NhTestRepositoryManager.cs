// <copyright file="NhTestRepositoryManager.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.NhSession;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NHibernate;

namespace FubarDev.UnitOfWork.Tests.Support
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NhTestRepositoryManager : NhRepositoryManager, IDisposable
    {
        private readonly DbConnection _connection;

        public NhTestRepositoryManager(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public override ISession Create()
        {
            return SessionFactory.WithOptions().Connection(_connection).OpenSession();
        }

        public override async ValueTask InitializeAsync(ISession repository, CancellationToken cancellationToken = default)
        {
            // We're using EF Core for simple table creation
            await using var dbContext = new TestDbContext();
            dbContext.Database.SetDbConnection(_connection);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
