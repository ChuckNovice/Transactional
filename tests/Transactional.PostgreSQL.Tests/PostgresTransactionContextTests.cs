namespace Transactional.PostgreSQL.Tests;

using System;
using System.Threading.Tasks;
using Xunit;

public class PostgresTransactionContextTests
{
    [Fact]
    public void Constructor_NullTransaction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresTransactionContext(null!));
    }

    // Note: NpgsqlTransaction and NpgsqlDataSource are sealed classes and cannot be mocked.
    // State management and lifecycle tests are best verified through integration tests.
    // The following tests verify argument validation and basic contract expectations.

    [Fact]
    public void Constructor_AcceptsTransaction_TypeCheck()
    {
        // This test documents the expected constructor signature.
        // Actual behavior testing requires integration tests with a real database.
        var constructorInfo = typeof(PostgresTransactionContext)
            .GetConstructor(new[] { typeof(Npgsql.NpgsqlTransaction) });

        Assert.NotNull(constructorInfo);
    }

    [Fact]
    public void Transaction_Property_Exists()
    {
        // Verify the Transaction property is accessible
        var propertyInfo = typeof(PostgresTransactionContext)
            .GetProperty(nameof(PostgresTransactionContext.Transaction));

        Assert.NotNull(propertyInfo);
        Assert.Equal(typeof(Npgsql.NpgsqlTransaction), propertyInfo.PropertyType);
    }

    [Fact]
    public void ImplementsIPostgresTransactionContext()
    {
        Assert.True(typeof(IPostgresTransactionContext).IsAssignableFrom(typeof(PostgresTransactionContext)));
    }

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(PostgresTransactionContext)));
    }

    [Fact]
    public async Task CommitAsync_HasExpectedSignature()
    {
        // Verify the method signature exists
        var methodInfo = typeof(PostgresTransactionContext)
            .GetMethod(nameof(PostgresTransactionContext.CommitAsync));

        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(Task), methodInfo.ReturnType);
    }

    [Fact]
    public async Task RollbackAsync_HasExpectedSignature()
    {
        // Verify the method signature exists
        var methodInfo = typeof(PostgresTransactionContext)
            .GetMethod(nameof(PostgresTransactionContext.RollbackAsync));

        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(Task), methodInfo.ReturnType);
    }

    // State property tests

    [Fact]
    public void IsCommitted_Property_Exists()
    {
        var propertyInfo = typeof(PostgresTransactionContext)
            .GetProperty(nameof(PostgresTransactionContext.IsCommitted));

        Assert.NotNull(propertyInfo);
        Assert.Equal(typeof(bool), propertyInfo.PropertyType);
        Assert.True(propertyInfo.CanRead);
    }

    [Fact]
    public void IsRolledBack_Property_Exists()
    {
        var propertyInfo = typeof(PostgresTransactionContext)
            .GetProperty(nameof(PostgresTransactionContext.IsRolledBack));

        Assert.NotNull(propertyInfo);
        Assert.Equal(typeof(bool), propertyInfo.PropertyType);
        Assert.True(propertyInfo.CanRead);
    }

    // Callback method tests

    [Fact]
    public void OnCommitted_HasExpectedSignature()
    {
        var methodInfo = typeof(PostgresTransactionContext)
            .GetMethod(nameof(PostgresTransactionContext.OnCommitted));

        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(void), methodInfo.ReturnType);

        var parameters = methodInfo.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(Func<System.Threading.CancellationToken, Task>), parameters[0].ParameterType);
    }

    [Fact]
    public void OnRolledBack_HasExpectedSignature()
    {
        var methodInfo = typeof(PostgresTransactionContext)
            .GetMethod(nameof(PostgresTransactionContext.OnRolledBack));

        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(void), methodInfo.ReturnType);

        var parameters = methodInfo.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(Func<System.Threading.CancellationToken, Task>), parameters[0].ParameterType);
    }
}
