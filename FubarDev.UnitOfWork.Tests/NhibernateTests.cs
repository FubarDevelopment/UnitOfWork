// <copyright file="NhibernateTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

using FubarDev.UnitOfWork.Tests.Support;
using FubarDev.UnitOfWork.Tests.Support.DbModels;

using Microsoft.Extensions.DependencyInjection;

using NHibernate;
using NHibernate.Linq;

using Xunit;

namespace FubarDev.UnitOfWork.Tests
{
    public class NhibernateTests : IAsyncLifetime
    {
        private static readonly ISessionFactory _sessionFactory =
            Fluently.Configure()
                .Database(MsSqliteConfiguration.Standard.InMemory())
                .Mappings(
                    m => m.AutoMappings.Add(
                        AutoMap.AssemblyOf<TestModel>()
                            .Where(t => t.Namespace == typeof(TestModel).Namespace)))
                .BuildSessionFactory();

        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory<ISession> _factory;

        public NhibernateTests()
        {
            var services = new ServiceCollection()
                .AddSingleton(_sessionFactory)
                .AddUnitOfWork<ISession>()
                .Use<NhTestRepositoryManager>();
            _serviceProvider = services.BuildServiceProvider(true);
            _factory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory<ISession>>();
        }

        public Task InitializeAsync()
        {
            // throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            _sessionFactory.Dispose();
        }

        [Fact]
        public async Task TestSimpleInsert()
        {
            await using var unitOfWork = await _factory.CreateTransactionalAsync();

            var countStart = await unitOfWork.Repository.Query<TestModel>().CountAsync();
            Assert.Equal(0, countStart);

            await unitOfWork.Repository.SaveAsync(
                new TestModel()
                {
                    Text = "text 1",
                });
            await unitOfWork.SaveChangesAsync();

            var countEnd = await unitOfWork.Repository.Query<TestModel>().CountAsync();
            Assert.Equal(1, countEnd);

            await unitOfWork.CommitAsync();
        }

        [Fact]
        public async Task TestInsertWithRollback()
        {
            await using var unitOfWork1 = await _factory.CreateAsync();
            await using var unitOfWork2 = await _factory.CreateTransactionalAsync();

            var countStart = await unitOfWork2.Repository.Query<TestModel>().CountAsync();
            Assert.Equal(0, countStart);

            await unitOfWork2.Repository.SaveAsync(
                new TestModel()
                {
                    Text = "text 1",
                });
            await unitOfWork2.SaveChangesAsync();

            var countEnd2 = await unitOfWork2.Repository.Query<TestModel>().CountAsync();
            Assert.Equal(1, countEnd2);

            await unitOfWork2.RollbackAsync();

            var countEnd1 = await unitOfWork1.Repository.Query<TestModel>().CountAsync();
            Assert.Equal(0, countEnd1);
        }

        [Fact]
        public async Task TestNestedTransaction()
        {
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    var countStart = await unitOfWork2.Repository.Query<TestModel>().CountAsync();
                    Assert.Equal(0, countStart);

                    await unitOfWork2.Repository.SaveAsync(
                        new TestModel()
                        {
                            Text = "text 1",
                        });

                    await unitOfWork2.CommitAsync();
                }

                var countEnd2 = await unitOfWork1.Repository.Query<TestModel>().CountAsync();
                Assert.Equal(1, countEnd2);

                await unitOfWork1.CommitAsync();
            }

            await using (var unitOfWork1 = await _factory.CreateAsync())
            {
                var countEnd1 = await unitOfWork1.Repository.Query<TestModel>().CountAsync();
                Assert.Equal(1, countEnd1);
            }
        }
    }
}
