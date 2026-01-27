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

### MongoDB

```csharp
// Program.cs - Register services
builder.Services.AddMongoDbTransactionManager(sp =>
    new MongoClient("mongodb://localhost:27017"));

// Usage
public class MyService
{
    private readonly IMongoTransactionManager _transactionManager;

    public MyService(IMongoTransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    public async Task DoWorkAsync()
    {
        await using var context = await _transactionManager.BeginTransactionAsync();

        // Pass context to repositories...
        await context.CommitAsync();
    }
}
```

### PostgreSQL

```csharp
// Program.cs - Register services
builder.Services.AddPostgresTransactionManager("Host=localhost;Database=myapp;Username=postgres;Password=secret");

// Usage
public class MyService
{
    private readonly IPostgresTransactionManager _transactionManager;

    public MyService(IPostgresTransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    public async Task DoWorkAsync()
    {
        await using var context = await _transactionManager.BeginTransactionAsync();

        // Pass context to repositories...
        await context.CommitAsync();
    }
}
```

## Packages

| Package | Description | Dependencies |
|---------|-------------|--------------|
| `Transactional.Abstractions` | Core interface (`ITransactionContext`) | None |
| `Transactional.MongoDB` | MongoDB implementation wrapping `IClientSessionHandle` | MongoDB.Driver 3.x |
| `Transactional.PostgreSQL` | PostgreSQL implementation wrapping `NpgsqlTransaction` | Npgsql 8.x |

## Dependency Injection Setup

### MongoDB

```csharp
// Option 1: With existing IMongoClient registration
builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
builder.Services.AddMongoDbTransactionManager();

// Option 2: With factory (registers both IMongoClient and transaction manager)
builder.Services.AddMongoDbTransactionManager(sp =>
    new MongoClient(configuration.GetConnectionString("MongoDB")));
```

### PostgreSQL

```csharp
// Option 1: With connection string (registers both NpgsqlDataSource and transaction manager)
builder.Services.AddPostgresTransactionManager("Host=localhost;Database=myapp");

// Option 2: With existing NpgsqlDataSource registration
builder.Services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=myapp"));
builder.Services.AddPostgresTransactionManager();

// Option 3: With factory (registers both NpgsqlDataSource and transaction manager)
builder.Services.AddPostgresTransactionManager(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return NpgsqlDataSource.Create(config.GetConnectionString("PostgreSQL")!);
});
```

## Recommended Usage Pattern

### Repository Layer

Repositories should accept the base `ITransactionContext` interface, then cast to the database-specific type internally. This keeps your repository contracts database-agnostic while still allowing full access to native transaction features.

```csharp
public class UserRepository
{
    private readonly IMongoDatabase _database;

    public UserRepository(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task CreateUserAsync(User user, ITransactionContext? transactionContext = null)
    {
        var collection = _database.GetCollection<User>("users");

        if (transactionContext != null)
        {
            if (transactionContext is not IMongoTransactionContext mongoContext)
            {
                throw new InvalidOperationException(
                    $"Expected {nameof(IMongoTransactionContext)} but received {transactionContext.GetType().Name}");
            }

            await collection.InsertOneAsync(mongoContext.Session, user);
        }
        else
        {
            await collection.InsertOneAsync(user);
        }
    }
}

public class OrderRepository
{
    private readonly IMongoDatabase _database;

    public OrderRepository(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task CreateOrderAsync(Order order, ITransactionContext? transactionContext = null)
    {
        var collection = _database.GetCollection<Order>("orders");

        if (transactionContext != null)
        {
            if (transactionContext is not IMongoTransactionContext mongoContext)
            {
                throw new InvalidOperationException(
                    $"Expected {nameof(IMongoTransactionContext)} but received {transactionContext.GetType().Name}");
            }

            await collection.InsertOneAsync(mongoContext.Session, order);
        }
        else
        {
            await collection.InsertOneAsync(order);
        }
    }
}
```

### Service Layer

The service layer controls transaction boundaries and passes the context to repositories:

```csharp
public class OrderService
{
    private readonly IMongoTransactionManager _transactionManager;
    private readonly UserRepository _userRepo;
    private readonly OrderRepository _orderRepo;

    public OrderService(
        IMongoTransactionManager transactionManager,
        UserRepository userRepo,
        OrderRepository orderRepo)
    {
        _transactionManager = transactionManager;
        _userRepo = userRepo;
        _orderRepo = orderRepo;
    }

    public async Task CreateOrderWithUserAsync(User user, Order order)
    {
        await using var context = await _transactionManager.BeginTransactionAsync();

        try
        {
            await _userRepo.CreateUserAsync(user, context);
            await _orderRepo.CreateOrderAsync(order, context);
            await context.CommitAsync();
        }
        catch
        {
            await context.RollbackAsync();
            throw;
        }
    }
}
```

### Transaction Scope Helper

A helper method that wraps the transaction lifecycle:

```csharp
public static class TransactionScope
{
    public static async Task ExecuteAsync(
        IMongoTransactionManager manager,
        Func<ITransactionContext, Task> action)
    {
        await using var context = await manager.BeginTransactionAsync();

        try
        {
            await action(context);
            await context.CommitAsync();
        }
        catch
        {
            await context.RollbackAsync();
            throw;
        }
    }
}

// Usage
await TransactionScope.ExecuteAsync(_transactionManager, async context =>
{
    await _userRepo.CreateUserAsync(user, context);
    await _orderRepo.CreateOrderAsync(order, context);
});
```

## PostgreSQL Repository Example

```csharp
public class UserRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task CreateUserAsync(string name, ITransactionContext? transactionContext = null)
    {
        if (transactionContext != null)
        {
            if (transactionContext is not IPostgresTransactionContext pgContext)
            {
                throw new InvalidOperationException(
                    $"Expected {nameof(IPostgresTransactionContext)} but received {transactionContext.GetType().Name}");
            }

            await using var command = pgContext.Transaction.Connection!.CreateCommand();
            command.Transaction = pgContext.Transaction;
            command.CommandText = "INSERT INTO users (name) VALUES (@name)";
            command.Parameters.AddWithValue("name", name);
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO users (name) VALUES (@name)";
            command.Parameters.AddWithValue("name", name);
            await command.ExecuteNonQueryAsync();
        }
    }
}
```

## Best Practices

1. **Always use `await using` for transactions** - This ensures proper disposal even if exceptions occur.

2. **Pass `ITransactionContext` to repositories** - Use the base interface in method signatures, then cast internally to keep contracts database-agnostic.

3. **Commit explicitly before dispose** - While dispose will rollback uncommitted transactions, it's clearer to commit explicitly.

4. **Handle exceptions appropriately** - Catch exceptions, rollback, and rethrow to ensure proper transaction cleanup.

5. **Make transaction participation optional** - Allow methods to work with or without a transaction context for flexibility.

6. **Use DI extension methods** - Prefer `AddMongoDbTransactionManager()` and `AddPostgresTransactionManager()` over manual registration.

## API Reference

All public APIs include XML documentation. Enable documentation generation in your IDE or refer to the generated XML documentation files in the NuGet packages.

### Core Interfaces

- `ITransactionContext` - Represents an active transaction with `CommitAsync` and `RollbackAsync`

### MongoDB

- `IMongoTransactionContext` - Extends `ITransactionContext`, exposes `Session` property
- `IMongoTransactionManager` - Creates transactions via `BeginTransactionAsync`

### PostgreSQL

- `IPostgresTransactionContext` - Extends `ITransactionContext`, exposes `Transaction` property
- `IPostgresTransactionManager` - Creates transactions via `BeginTransactionAsync`

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details.
