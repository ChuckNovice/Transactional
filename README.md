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
builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
builder.Services.AddMongoDbTransactionManager();

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
// Register IMongoClient first, then add transaction manager
builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(configuration.GetConnectionString("MongoDB")));
builder.Services.AddMongoDbTransactionManager();
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

        await _userRepo.CreateUserAsync(user, context);
        await _orderRepo.CreateOrderAsync(order, context);

        await context.CommitAsync();
        // If an exception is thrown before CommitAsync, DisposeAsync will auto-rollback
    }
}
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

1. **Always use `await using` for transactions** - This ensures proper disposal and automatic rollback if an exception occurs before commit.

2. **Pass `ITransactionContext` to repositories** - Use the base interface in method signatures, then cast internally to keep contracts database-agnostic.

3. **Commit explicitly** - Call `CommitAsync()` when your work is complete. Uncommitted transactions are automatically rolled back on dispose.

4. **Make transaction participation optional** - Allow methods to work with or without a transaction context for flexibility.

5. **Use DI extension methods** - Prefer `AddMongoDbTransactionManager()` and `AddPostgresTransactionManager()` over manual registration.

## Advanced Features

### Transaction State

Check whether a transaction has been committed or rolled back:

```csharp
await using var context = await _transactionManager.BeginTransactionAsync();

// ... do work ...

if (!context.IsCommitted && !context.IsRolledBack)
{
    await context.CommitAsync();
}
```

### Commit/Rollback Callbacks

Register async callbacks that execute after a transaction completes:

```csharp
await using var context = await _transactionManager.BeginTransactionAsync();

// Register callbacks before commit/rollback
context.OnCommitted(async cancellationToken =>
{
    // Runs after successful commit - e.g., publish events, send notifications
    await _eventPublisher.PublishAsync(new OrderCreatedEvent(order), cancellationToken);
});

context.OnRolledBack(async cancellationToken =>
{
    // Runs after explicit rollback - e.g., cleanup, logging
    _logger.LogWarning("Order creation rolled back");
});

await _orderRepo.CreateOrderAsync(order, context);
await context.CommitAsync(); // OnCommitted callbacks fire after this completes
```

**Note:** Callbacks only fire for explicit `CommitAsync()` or `RollbackAsync()` calls. They do **not** fire when `DisposeAsync()` performs an implicit rollback.

### Wrapping Existing Transactions

Wrap a pre-existing database transaction into a context. The wrapped transaction's lifecycle remains your responsibility:

```csharp
// MongoDB: Wrap a session that already has an active transaction
using var session = await client.StartSessionAsync();
session.StartTransaction();

// ... do some work directly with session ...

// Now wrap it to pass to libraries expecting ITransactionContext
var context = _transactionManager.WrapExistingTransaction(session);
await _someLibrary.DoWorkAsync(context);
await context.CommitAsync();

// session is NOT disposed when context is disposed - you manage it
session.Dispose();
```

```csharp
// PostgreSQL: Wrap an existing NpgsqlTransaction
await using var connection = await _dataSource.OpenConnectionAsync();
await using var transaction = await connection.BeginTransactionAsync();

var context = _transactionManager.WrapExistingTransaction(transaction);
await _someLibrary.DoWorkAsync(context);
await context.CommitAsync();

// transaction/connection are NOT disposed when context is disposed
```

## API Reference

All public APIs include XML documentation. Enable documentation generation in your IDE or refer to the generated XML documentation files in the NuGet packages.

### ITransactionContext

Base interface for all transaction contexts (extends `IAsyncDisposable`):

| Member | Description |
|--------|-------------|
| `IsCommitted` | `true` after successful commit |
| `IsRolledBack` | `true` after explicit rollback |
| `CommitAsync(CancellationToken)` | Commits the transaction |
| `RollbackAsync(CancellationToken)` | Rolls back the transaction |
| `OnCommitted(Func<CancellationToken, Task>)` | Registers a post-commit callback |
| `OnRolledBack(Func<CancellationToken, Task>)` | Registers a post-rollback callback |

### MongoDB

**IMongoTransactionContext** - Extends `ITransactionContext`:
- `Session` - The underlying `IClientSessionHandle`

**IMongoTransactionManager**:
- `BeginTransactionAsync(TransactionOptions?, CancellationToken)` - Starts a new transaction
- `WrapExistingTransaction(IClientSessionHandle)` - Wraps a session with an active transaction

### PostgreSQL

**IPostgresTransactionContext** - Extends `ITransactionContext`:
- `Transaction` - The underlying `NpgsqlTransaction`

**IPostgresTransactionManager**:
- `BeginTransactionAsync(IsolationLevel, CancellationToken)` - Starts a new transaction (default: ReadCommitted)
- `WrapExistingTransaction(NpgsqlTransaction)` - Wraps an existing transaction

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details.
