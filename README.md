# Transactional

A .NET 10 library providing database-agnostic transaction abstractions. It enables multiple independent libraries to participate in a single shared database transaction without tight coupling to specific database implementations.

## Overview

Transactional solves the problem of coordinating transactions across multiple libraries that need to work with the same database. Instead of each library managing its own transactions, they can participate in a shared transaction context that is controlled at the application level.

## Installation

```bash
# Core abstractions (no external dependencies)
dotnet add package Transactional.Abstractions

# MongoDB implementation (requires MongoDB.Driver 3.x)
dotnet add package Transactional.MongoDB

# PostgreSQL implementation (requires Npgsql 8.x)
dotnet add package Transactional.PostgreSQL
```

## Quick Start

```csharp
// MongoDB example
var client = new MongoClient("mongodb://localhost:27017");
var manager = new MongoTransactionManager(client);

await using (var context = await manager.BeginTransactionAsync())
{
    var collection = database.GetCollection<BsonDocument>("users");
    await collection.InsertOneAsync(context.Session, new BsonDocument("name", "John"));
    await context.CommitAsync();
}
```

## Packages

| Package | Description | Dependencies |
|---------|-------------|--------------|
| `Transactional.Abstractions` | Core interfaces (`ITransactionContext`, `ITransactionContextAccessor`) | None |
| `Transactional.MongoDB` | MongoDB implementation wrapping `IClientSessionHandle` | MongoDB.Driver 3.x |
| `Transactional.PostgreSQL` | PostgreSQL implementation wrapping `NpgsqlTransaction` | Npgsql 8.x |

## Usage Patterns

### Pattern 1: Repository Participation

Repositories check for an ambient transaction and use it if available:

```csharp
public class UserRepository
{
    private readonly IMongoDatabase _database;
    private readonly ITransactionContextAccessor _accessor;

    public UserRepository(IMongoDatabase database, ITransactionContextAccessor accessor)
    {
        _database = database;
        _accessor = accessor;
    }

    public async Task CreateUserAsync(User user)
    {
        var collection = _database.GetCollection<User>("users");
        var session = (_accessor.Current as IMongoTransactionContext)?.Session;

        if (session != null)
        {
            await collection.InsertOneAsync(session, user);
        }
        else
        {
            await collection.InsertOneAsync(user);
        }
    }
}
```

### Pattern 2: Application-Level Transaction Control

The application controls when transactions begin and end:

```csharp
public class OrderService
{
    private readonly IMongoTransactionManager _transactionManager;
    private readonly ITransactionContextAccessor _accessor;
    private readonly UserRepository _userRepo;
    private readonly OrderRepository _orderRepo;

    public async Task CreateOrderWithUserAsync(User user, Order order)
    {
        await using var context = await _transactionManager.BeginTransactionAsync();
        _accessor.Current = context;

        try
        {
            await _userRepo.CreateUserAsync(user);
            await _orderRepo.CreateOrderAsync(order);
            await context.CommitAsync();
        }
        catch
        {
            await context.RollbackAsync();
            throw;
        }
        finally
        {
            _accessor.Current = null;
        }
    }
}
```

### Pattern 3: Transaction Scope Helper

A helper method that wraps the transaction lifecycle:

```csharp
public static class TransactionScope
{
    public static async Task ExecuteAsync(
        IMongoTransactionManager manager,
        ITransactionContextAccessor accessor,
        Func<Task> action)
    {
        await using var context = await manager.BeginTransactionAsync();
        accessor.Current = context;

        try
        {
            await action();
            await context.CommitAsync();
        }
        catch
        {
            await context.RollbackAsync();
            throw;
        }
        finally
        {
            accessor.Current = null;
        }
    }
}

// Usage
await TransactionScope.ExecuteAsync(_manager, _accessor, async () =>
{
    await _userRepo.CreateUserAsync(user);
    await _orderRepo.CreateOrderAsync(order);
});
```

## MongoDB Example

```csharp
using MongoDB.Driver;
using Transactional.MongoDB;

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("myapp");
var manager = new MongoTransactionManager(client);

// With custom transaction options
var options = new TransactionOptions(
    readConcern: ReadConcern.Majority,
    writeConcern: WriteConcern.WMajority);

await using (var context = await manager.BeginTransactionAsync(options))
{
    var users = database.GetCollection<BsonDocument>("users");
    var orders = database.GetCollection<BsonDocument>("orders");

    // Use context.Session for all operations
    await users.InsertOneAsync(context.Session, new BsonDocument("name", "Alice"));
    await orders.InsertOneAsync(context.Session, new BsonDocument("userId", "alice"));

    await context.CommitAsync();
}
```

## PostgreSQL Example

```csharp
using Npgsql;
using Transactional.PostgreSQL;
using System.Data;

var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=myapp;Username=postgres;Password=secret");
var manager = new PostgresTransactionManager(dataSource);

// With specific isolation level
await using (var context = await manager.BeginTransactionAsync(IsolationLevel.Serializable))
{
    await using var command = context.Transaction.Connection!.CreateCommand();
    command.Transaction = context.Transaction;

    command.CommandText = "INSERT INTO users (name) VALUES (@name)";
    command.Parameters.AddWithValue("name", "Alice");
    await command.ExecuteNonQueryAsync();

    command.CommandText = "INSERT INTO orders (user_id) VALUES (@userId)";
    command.Parameters.Clear();
    command.Parameters.AddWithValue("userId", 1);
    await command.ExecuteNonQueryAsync();

    await context.CommitAsync();
}
```

## Dependency Injection Setup

### ASP.NET Core with MongoDB

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient("mongodb://localhost:27017"));

builder.Services.AddSingleton<IMongoTransactionManager, MongoTransactionManager>();
builder.Services.AddScoped<ITransactionContextAccessor, TransactionContextAccessor>();

// Simple accessor implementation
public class TransactionContextAccessor : ITransactionContextAccessor
{
    public ITransactionContext? Current { get; set; }
}
```

### ASP.NET Core with PostgreSQL

```csharp
builder.Services.AddSingleton(sp =>
    NpgsqlDataSource.Create("Host=localhost;Database=myapp;Username=postgres;Password=secret"));

builder.Services.AddSingleton<IPostgresTransactionManager, PostgresTransactionManager>();
builder.Services.AddScoped<ITransactionContextAccessor, TransactionContextAccessor>();
```

## Best Practices

1. **Always use `await using` for transactions** - This ensures proper disposal even if exceptions occur.

2. **Register `ITransactionContextAccessor` as Scoped** - This ensures each request/scope has its own transaction context.

3. **Commit explicitly before dispose** - While dispose will rollback uncommitted transactions, it's clearer to commit explicitly.

4. **Clear the accessor after transaction completes** - Set `accessor.Current = null` after commit or rollback.

5. **Handle exceptions appropriately** - Catch exceptions, rollback, and rethrow to ensure proper transaction cleanup.

## API Reference

All public APIs include XML documentation. Enable documentation generation in your IDE or refer to the generated XML documentation files in the NuGet packages.

### Core Interfaces

- `ITransactionContext` - Represents an active transaction with `CommitAsync` and `RollbackAsync`
- `ITransactionContextAccessor` - Holds the ambient transaction via the `Current` property

### MongoDB

- `IMongoTransactionContext` - Extends `ITransactionContext`, exposes `Session` property
- `IMongoTransactionManager` - Creates transactions via `BeginTransactionAsync`

### PostgreSQL

- `IPostgresTransactionContext` - Extends `ITransactionContext`, exposes `Transaction` property
- `IPostgresTransactionManager` - Creates transactions via `BeginTransactionAsync`

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
