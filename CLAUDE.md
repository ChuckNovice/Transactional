# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Transactional is a .NET 10 library providing database-agnostic transaction abstractions. It enables multiple independent libraries to participate in a single shared database transaction without tight coupling to specific database implementations.

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run unit tests
dotnet test

# Run a specific test project
dotnet test tests/Transactional.Abstractions.Tests

# Build and pack for release (version set by CI)
dotnet pack -c Release /p:Version={version}
```

## Architecture

Three NuGet packages with a layered dependency structure:

- **Transactional.Abstractions** - Core interface (`ITransactionContext`) with no external dependencies
- **Transactional.MongoDB** - MongoDB implementation wrapping `IClientSessionHandle`, depends on MongoDB.Driver 3.x
- **Transactional.PostgreSQL** - PostgreSQL implementation wrapping `NpgsqlTransaction`, depends on Npgsql 8.x

Key interfaces:
- `ITransactionContext` (extends `IAsyncDisposable`): CommitAsync, RollbackAsync - represents an active transaction
- Database-specific interfaces (`IMongoTransactionContext`, `IPostgresTransactionContext`) expose native transaction objects

Usage pattern: Transaction contexts should be passed directly to methods that need to participate in the transaction, rather than retrieved from ambient state.

## Testing Strategy

- **Unit tests**: Run in CI/CD on PR and tag push; must pass before merge
- **Integration tests**: Run locally only, require environment variables:
  - `MONGODB_CONNECTION_STRING` for MongoDB tests
  - `POSTGRES_CONNECTION_STRING` for PostgreSQL tests
  - Tests skip automatically if connection string not set

## Versioning and Release

- Version derived from Git tags (never hardcoded in .csproj)
- Format: `vMAJOR.MINOR.PATCH` (stable) or `vMAJOR.MINOR.PATCH-SUFFIX.NUMBER` (prerelease)
- MAJOR matches .NET version (10 for .NET 10)
- All packages share the same version and release together
- Tag push triggers GitHub Action: runs tests, builds, publishes to NuGet.org

## Branch Strategy

- `main`: Protected, requires PR for all changes
- Feature branches: `feature/[name]`, deleted after merge

## Code Standards

- XML documentation required on all public classes, methods, and properties
- `using` directives should be placed inside the namespace
