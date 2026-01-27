# Contributing to Transactional

Thank you for your interest in contributing to Transactional! This document provides guidelines and instructions for contributing.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- (Optional) Docker for running integration tests locally

### Cloning the Repository

```bash
git clone https://github.com/ChuckNovice/Transactional.git
cd Transactional
```

### Restoring Packages

```bash
dotnet restore
```

### Building

```bash
dotnet build
```

## Running Tests

### Unit Tests

Unit tests run without any external dependencies:

```bash
dotnet test --filter 'Category!=Integration'
```

### Integration Tests

Integration tests require running MongoDB and/or PostgreSQL instances.

#### Setting Up Databases with Docker

```bash
# MongoDB
docker run -d --name mongo-test -p 27017:27017 mongo:7

# PostgreSQL
docker run -d --name postgres-test -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16
```

#### Environment Variables

Create a `.env` file based on `.env.example`:

```bash
cp .env.example .env
```

Then edit `.env` with your connection strings:

```
MONGODB_CONNECTION_STRING=mongodb://localhost:27017/transactional_tests
POSTGRES_CONNECTION_STRING=Host=localhost;Database=transactional_tests;Username=postgres;Password=postgres
```

#### Running Integration Tests

```bash
# Source the environment variables (Linux/macOS)
export $(cat .env | xargs)

# Or on Windows PowerShell
Get-Content .env | ForEach-Object { if ($_ -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($matches[1], $matches[2]) } }

# Run integration tests
dotnet test --filter 'Category=Integration'
```

## Building Locally

### Debug Build

```bash
dotnet build
```

### Release Build with Pack

```bash
dotnet build -c Release
dotnet pack -c Release -p:Version=1.0.0-local
```

The `.nupkg` files will be created in `src/*/bin/Release/`.

## Code Standards

### XML Documentation

All public classes, methods, and properties must have XML documentation comments:

```csharp
/// <summary>
/// Brief description of the class.
/// </summary>
public class MyClass
{
    /// <summary>
    /// Brief description of the method.
    /// </summary>
    /// <param name="value">Description of parameter.</param>
    /// <returns>Description of return value.</returns>
    public int MyMethod(string value) { ... }
}
```

### Using Directives

Place `using` directives inside the namespace:

```csharp
namespace Transactional.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface ITransactionContext { ... }
}
```

### Additional Guidelines

- Use `async`/`await` throughout
- Include `CancellationToken` parameters on async methods
- Use `ConfigureAwait(false)` in library code
- Validate arguments and throw `ArgumentNullException` for null parameters
- Make idempotent operations truly idempotent

## Branching Strategy

- `main` - Protected branch, requires PR for all changes
- `feature/*` - Feature branches for new functionality
- `fix/*` - Bug fix branches

### Creating a Feature Branch

```bash
git checkout main
git pull origin main
git checkout -b feature/my-new-feature
```

### Submitting a Pull Request

1. Push your branch to GitHub
2. Create a Pull Request against `main`
3. Ensure all CI checks pass
4. Request review from maintainers

## Versioning and Releases

### Version Format

- Stable: `vMAJOR.MINOR.PATCH` (e.g., `v10.0.0`)
- Prerelease: `vMAJOR.MINOR.PATCH-SUFFIX.NUMBER` (e.g., `v10.0.0-beta.1`)

The MAJOR version matches the .NET version (10 for .NET 10).

### Creating a Release

Releases are triggered by pushing a Git tag:

```bash
# Ensure you're on main with latest changes
git checkout main
git pull origin main

# Create an annotated tag
git tag -a v10.0.1 -m "Release v10.0.1 - Bug fixes"

# Push the tag
git push origin v10.0.1
```

The GitHub Actions workflow will automatically:
1. Build the projects
2. Run unit tests
3. Create NuGet packages
4. Publish to NuGet.org

## Questions?

If you have questions, please open an issue on GitHub.
