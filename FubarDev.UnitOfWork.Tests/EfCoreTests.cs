// <copyright file="EfCoreTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Data.Common;
using System.Threading.Tasks;

using FubarDev.UnitOfWork.Tests.Support;
using FubarDev.UnitOfWork.Tests.Support.DbModels;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace FubarDev.UnitOfWork.Tests
{
    public class EfCoreTests : IAsyncLifetime
    {
        private readonly DbConnection _connection;
        private readonly ServiceProvider _serviceProvider;
        private readonly IUnitOfWorkFactory<TestDbContext> _factory;

        public EfCoreTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            var services = new ServiceCollection()
                .AddDbContextFactory<TestDbContext>(dcob =>
                {
                    dcob.UseSqlite(_connection);
                })
                .AddUnitOfWork<TestDbContext>()
                .Use<EfCoreTestRepositoryManager>();
            _serviceProvider = services.BuildServiceProvider(true);
            _factory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory<TestDbContext>>();
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }

        [Fact]
        public async Task TestSimpleInsert()
        {
            await using var unitOfWork = await _factory.CreateTransactionalAsync();

            var countStart = await unitOfWork.Repository.TestModels.CountAsync();
            Assert.Equal(0, countStart);

            await unitOfWork.Repository.TestModels.AddAsync(
                new TestModel()
                {
                    Text = "text 1",
                });
            await unitOfWork.SaveChangesAsync();

            var countEnd = await unitOfWork.Repository.TestModels.CountAsync();
            Assert.Equal(1, countEnd);

            await unitOfWork.CommitAsync();
        }

        [Fact]
        public async Task TestInsertWithRollback()
        {
            await using var unitOfWork1 = await _factory.CreateAsync();
            await using var unitOfWork2 = await _factory.CreateTransactionalAsync();

            var countStart = await unitOfWork2.Repository.TestModels.CountAsync();
            Assert.Equal(0, countStart);

            await unitOfWork2.Repository.TestModels.AddAsync(
                new TestModel()
                {
                    Text = "text 1",
                });
            await unitOfWork2.SaveChangesAsync();

            var countEnd2 = await unitOfWork2.Repository.TestModels.CountAsync();
            Assert.Equal(1, countEnd2);

            await unitOfWork2.RollbackAsync();

            var countEnd1 = await unitOfWork1.Repository.TestModels.CountAsync();
            Assert.Equal(0, countEnd1);
        }

        [Fact]
        public async Task TestNestedTransaction()
        {
            await using (var unitOfWork1 = await _factory.CreateTransactionalAsync())
            {
                await using (var unitOfWork2 = await _factory.CreateTransactionalAsync())
                {
                    var countStart = await unitOfWork2.Repository.TestModels.CountAsync();
                    Assert.Equal(0, countStart);

                    await unitOfWork2.Repository.TestModels.AddAsync(
                        new TestModel()
                        {
                            Text = "text 1",
                        });

                    await unitOfWork2.CommitAsync();
                }

                var countEnd2 = await unitOfWork1.Repository.TestModels.CountAsync();
                Assert.Equal(1, countEnd2);

                await unitOfWork1.CommitAsync();
            }

            await using (var unitOfWork1 = await _factory.CreateAsync())
            {
                var countEnd1 = await unitOfWork1.Repository.TestModels.CountAsync();
                Assert.Equal(1, countEnd1);
            }
        }
    }
}
