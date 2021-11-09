# UnitOfWork

Scoped unit-of-work implementation that supports Entity Framework Core and NHibernate out of the box.

# Usage with NHibernate

## Add nuget package

```shell
dotnet add package FubarDev.UnitOfWork.NhSession
```

## Configure services

The `yourSessionFactory` must already contain an instance of `ISessionFactory`.

```c#
var services = new ServiceCollection();
services.AddSingleton(yourSessionFactory)
    .AddUnitOfWork<ISession>()
    .Use<NhRepositoryManager>();
var serviceProvider = services.BuildServiceProvider();
```

## Create a unit of work

```c#
class YourService
{
    private readonly IUnitOfWorkFactory<ISession> _factory;

    public YourService(IUnitOfWorkFactory<ISession> factory)
    {
        _factory = factory;
    }
    
    public async Task YourTransactionalServiceOperationAsync(CancellationToken cancellationToken)
    {
        // Create a new unit of work (with transaction)
        await using var unitOfWork = await _factory.CreateTransactionalAsync(cancellationToken);
        
        // Access the NHibernate session and do something with it
        var session = unitOfWork.Repository;

        // Commit all changes
        await unitOfWork.CommitAsync(cancellationToken);
    } 
}
```

# Usage with Entity Framework Core

## Add nuget package

```shell
dotnet add package FubarDev.UnitOfWork.EfCore
```

## Configure services

The `yourSessionFactory` must already contain an instance of `ISessionFactory`.

```c#
var services = new ServiceCollection();
services
    .AddDbContextFactory<YourDbContext>(dcob =>
    {
        // Configure EF Core options
        dcob.UseSqlite(_connection);
    })
    .AddUnitOfWork<YourDbContext>()
    .Use<EfCoreRepositoryManager<YourDbContext>>();
var serviceProvider = services.BuildServiceProvider();
```

## Create a unit of work

```c#
class YourService
{
    private readonly IUnitOfWorkFactory<YourDbContext> _factory;

    public YourService(IUnitOfWorkFactory<YourDbContext> factory)
    {
        _factory = factory;
    }
    
    public async Task YourTransactionalServiceOperationAsync(CancellationToken cancellationToken)
    {
        // Create a new unit of work (with transaction)
        await using var unitOfWork = await _factory.CreateTransactionalAsync(cancellationToken);
        
        // Access the DB context and do something with it
        var dbContext = unitOfWork.Repository;

        // Commit all changes
        await unitOfWork.CommitAsync(cancellationToken);
    } 
}
```
