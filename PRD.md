# Product Requirements Document: Transactional

## Overview

**Product Name**: Transactional
**Repository**: Single repository, multiple NuGet packages
**Target Framework**: .NET 10
**Package Type**: NuGet Library Collection

## Purpose

Provide a lightweight, database-agnostic transaction abstraction that enables multiple independent libraries to participate in a single shared database transaction without tight coupling to specific database implementations.

## Problem Statement

Modern applications often use multiple libraries that each persist data to a database. When operations across these libraries must succeed or fail atomically, coordinating a single transaction becomes complex because:

1. Core libraries should remain database-agnostic
2. Different database providers have different transaction APIs
3. Passing database-specific transaction objects through method chains creates tight coupling
4. Multiple libraries need to share the same transaction instance without knowing about each other

## Goals

1. Enable transaction coordination across independent libraries
2. Maintain database abstraction in domain/business logic layers
3. Provide full access to database-specific transaction features
4. Minimize boilerplate code in consuming applications
5. Support MongoDB and PostgreSQL database providers

## Non-Goals

1. Replace existing ORM transaction mechanisms (Entity Framework, Dapper, etc.)
2. Provide database connection management
3. Implement distributed transactions or two-phase commit
4. Handle transaction retry logic or error handling policies
5. Support databases other than MongoDB and PostgreSQL

## Architecture

### Package Structure

**Transactional.Abstractions**
- Core interfaces with no external dependencies
- Database-agnostic contracts

**Transactional.MongoDB**
- MongoDB-specific implementation
- Depends on: Transactional.Abstractions, MongoDB.Driver

**Transactional.PostgreSQL**
- PostgreSQL-specific implementation
- Depends on: Transactional.Abstractions, Npgsql

## Technical Requirements

### Transactional.Abstractions

#### ITransactionContext Interface

**Extends**: IAsyncDisposable

**Public members:**
- `Task CommitAsync(CancellationToken cancellationToken = default)`
- `Task RollbackAsync(CancellationToken cancellationToken = default)`
- `ValueTask DisposeAsync()` (from IAsyncDisposable)

**Purpose**: Represents an active transaction that can be committed or rolled back.

**Behavior**:
- CommitAsync: Persists all changes made within the transaction
- RollbackAsync: Discards all changes made within the transaction
- DisposeAsync: Cleans up transaction resources, rolls back if not committed
- Both Commit and Rollback are idempotent (safe to call multiple times)
- Calling Commit after Rollback (or vice versa) should throw InvalidOperationException

### Transactional.MongoDB

#### IMongoTransactionContext Interface

**Extends**: ITransactionContext

**Additional members:**
- `IClientSessionHandle Session { get; }`

**Purpose**: Provides access to the native MongoDB session for database operations.

#### IMongoTransactionManager Interface

**Public members:**
- `Task<IMongoTransactionContext> BeginTransactionAsync(TransactionOptions? options = null, CancellationToken cancellationToken = default)`

**Purpose**: Creates MongoDB transactions with full access to MongoDB-specific options.

**Parameters**:
- options: MongoDB.Driver.TransactionOptions (read concern, write concern, read preference)
- cancellationToken: Standard cancellation token

#### MongoTransactionContext Class

**Responsibilities**:
- Wraps IClientSessionHandle from MongoDB.Driver
- Implements both ITransactionContext and IMongoTransactionContext
- Implements IAsyncDisposable
- Starts transaction on construction
- Delegates Commit/Rollback to underlying session
- Disposes session on DisposeAsync

#### MongoTransactionManager Class

**Responsibilities**:
- Creates IClientSessionHandle from IMongoClient
- Wraps session in MongoTransactionContext
- Returns transaction ready for use

**Dependencies**:
- IMongoClient (injected)

### Transactional.PostgreSQL

#### IPostgresTransactionContext Interface

**Extends**: ITransactionContext

**Additional members:**
- `NpgsqlTransaction Transaction { get; }`

**Purpose**: Provides access to the native Npgsql transaction for database operations.

#### IPostgresTransactionManager Interface

**Public members:**
- `Task<IPostgresTransactionContext> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)`

**Purpose**: Creates PostgreSQL transactions with standard isolation level options.

#### PostgresTransactionContext Class

**Responsibilities**:
- Wraps NpgsqlTransaction
- Implements both ITransactionContext and IPostgresTransactionContext
- Implements IAsyncDisposable
- Manages connection lifecycle if needed
- Delegates Commit/Rollback to underlying transaction
- Disposes transaction and connection on DisposeAsync

#### PostgresTransactionManager Class

**Responsibilities**:
- Creates NpgsqlTransaction from connection
- Wraps transaction in PostgresTransactionContext
- Returns transaction ready for use

**Dependencies**:
- NpgsqlDataSource or connection factory (injected)

## Usage Pattern

### Recommended: Explicit Context Passing

Pass the transaction context directly to methods that need to participate in the transaction. This makes transaction participation explicit and testable.

**Repository Pattern Example:**
```csharp
public async Task CreateUserAsync(User user, IMongoTransactionContext? context = null)
{
    var collection = _database.GetCollection<User>("users");
    if (context != null)
        await collection.InsertOneAsync(context.Session, user);
    else
        await collection.InsertOneAsync(user);
}
```

**Service Layer Example:**
```csharp
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
```

## API Design Principles

1. **Minimal Surface Area**: Abstractions contain only essential methods
2. **Database Feature Preservation**: Database-specific managers expose all native transaction options
3. **Fail-Safe Defaults**: Transactions default to safe settings (e.g., ReadCommitted isolation)
4. **Explicit Over Implicit**: No automatic transaction creation; application code controls lifecycle
5. **Explicit Context Passing**: Transaction context should be passed directly to methods, not retrieved from ambient state
6. **Proper Resource Management**: All transaction contexts implement IAsyncDisposable

## Dependencies

### Transactional.Abstractions
- None (pure interfaces)

### Transactional.MongoDB
- Transactional.Abstractions
- MongoDB.Driver (latest 3.x)

### Transactional.PostgreSQL
- Transactional.Abstractions
- Npgsql (latest 8.x)

## Testing Requirements

### Unit Tests

**Scope**: All public APIs and core logic

**Execution**:
- Run locally during development
- Run in GitHub Actions on PR and tag push
- Must pass before merge to `main`
- Must pass before package publishing

**Coverage Requirements**:
- All public interfaces and classes
- Transaction lifecycle (begin, commit, rollback, dispose)
- Error handling and edge cases

### Integration Tests

**Scope**: Actual database operations

**Execution**:
- Run locally only (not in CI/CD)
- Must pass before committing code
- Developer responsibility to validate

**Configuration**:
- Connection strings from environment variables:
  - `MONGODB_CONNECTION_STRING` for MongoDB tests
  - `POSTGRES_CONNECTION_STRING` for PostgreSQL tests
- Tests skipped if environment variable not set

**Test Coverage**:
- Transaction commit persists data
- Transaction rollback discards data
- Multiple operations in single transaction
- Concurrent transaction isolation
- Disposal without commit/rollback

## Repository Structure

### Branches
- **main**: Protected branch, single source of truth, requires PR for all changes
- **feature/[name]**: Temporary branches for development work

### Branch Protection Rules
- Direct commits to `main` prohibited
- All changes must go through Pull Request review process
- Unit tests must pass before merge
- Feature branches deleted after merge

## Versioning Scheme

### Format
- **Stable Release**: `vMAJOR.MINOR.PATCH` (e.g., `v10.1.0`)
- **Prerelease**: `vMAJOR.MINOR.PATCH-SUFFIX.NUMBER` (e.g., `v10.1.0-beta.1`)

### Semantic Versioning
- **MAJOR**: .NET target framework version (10 for .NET 10)
- **MINOR**: New features, backwards compatible
- **PATCH**: Bug fixes, backwards compatible
- **SUFFIX**: Prerelease identifier (`alpha`, `beta`, `rc`)

### Examples
- `v10.0.0` - First release for .NET 10
- `v10.1.0` - Feature addition
- `v10.1.1` - Bug fix
- `v10.2.0-beta.1` - First beta for upcoming feature

**Note**: All packages in the repository share the same version number and are released together.

## Development Workflow

### Daily Development
1. Create feature branch from `main`
2. Implement changes and commit to feature branch
3. Run unit tests locally
4. Run integration tests locally (must pass)
5. Push feature branch and create Pull Request
6. Unit tests run in GitHub Actions
7. Code review and approval
8. Merge PR to `main`
9. Delete feature branch
10. **No automatic publishing occurs**

### Publishing Prerelease
1. Ensure `main` is up to date
2. Ensure all tests pass
3. Create annotated tag: `git tag -a v10.1.0-beta.1 -m "Beta release"`
4. Push tag: `git push origin v10.1.0-beta.1`
5. GitHub Action runs unit tests, builds, and publishes all packages with prerelease flag
6. If issues found, fix on `main` and create new prerelease tag

### Publishing Stable Release
1. Ensure `main` is up to date
2. Ensure all tests pass
3. Create annotated tag: `git tag -a v10.1.0 -m "Stable release"`
4. Push tag: `git push origin v10.1.0`
5. GitHub Action runs unit tests, builds, and publishes all packages as stable

## CI/CD Implementation

### GitHub Action Workflow
**Location**: `.github/workflows/publish.yml`

**Trigger**: Push of any tag matching `v[0-9]+.[0-9]+.[0-9]+*`

**Steps**:
1. Checkout repository
2. Setup .NET 10 SDK
3. Restore dependencies
4. Run unit tests (fail pipeline if tests fail)
5. Extract version from tag (remove `refs/tags/v` prefix)
6. Build and pack all projects: `dotnet pack -c Release /p:Version={version}`
7. Push all packages to NuGet.org using `--skip-duplicate`

**Secrets Required**:
- `NUGET_API_KEY`: API key for NuGet.org

**Note**: Integration tests are NOT run in CI/CD, only locally.

### Project Configuration (.csproj)

**Required Properties**:
- `TargetFramework`: net10.0
- `PackageId`: Unique identifier (e.g., `Transactional.Abstractions`)
- `Authors`: ChuckNovice
- `Description`: Database transaction abstractions and implementations for coordinating operations across multiple persistence libraries in .NET

**Excluded Properties**:
- `Version`: Set dynamically by CI/CD, NOT hardcoded

**Optional Properties**:
- `PackageTags`: Keywords for discoverability
- `RepositoryUrl`: GitHub repository link
- `PackageLicenseExpression`: License (Apache-2.0)

## Key Principles

1. **Git Tag as Single Source of Truth**: Version always derived from Git tag
2. **Unified Versioning**: All packages released together with same version
3. **Tag Pattern Controls Release Type**: Suffix determines stable vs prerelease
4. **Manual Tag Creation**: Deliberate action prevents accidental releases
5. **NuGet.org Auto-Detection**: Platform marks `-alpha`, `-beta`, `-rc` as prereleases
6. **Test-Driven Quality**: Unit tests must pass before any release

## Distribution

- NuGet packages for each project
- All packages versioned identically (e.g., all 10.1.0)
- Transactional.Abstractions always required
- Database-specific packages optional based on choice

## Success Criteria

1. Libraries participate in transactions without database-specific dependencies
2. Application code accesses full database transaction features
3. Zero performance overhead vs native transactions
4. All releases traceable to Git commits via tags
5. Zero manual intervention after tag push
6. Clear distinction between stable and prerelease packages
7. Works seamlessly with dependency injection
8. All unit tests pass in CI/CD
9. Integration tests validate real-world scenarios

## Future Considerations

1. Transaction events/hooks for logging
2. Nested transaction support (savepoints)
3. OpenTelemetry integration

## Documentation Requirements

1. README with quick start examples
2. API reference documentation (XML comments)
3. Sample projects demonstrating common patterns
