namespace Transactional.PostgreSQL.Tests;

using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

public class PostgresTransactionManagerTests
{
    [Fact]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresTransactionManager(null!));
    }

    // Note: NpgsqlDataSource is a sealed class and cannot be mocked.
    // Behavior tests are best verified through integration tests with a real database.
    // The following tests verify argument validation and basic contract expectations.

    [Fact]
    public void Constructor_AcceptsDataSource_TypeCheck()
    {
        // This test documents the expected constructor signature.
        var constructorInfo = typeof(PostgresTransactionManager)
            .GetConstructor(new[] { typeof(Npgsql.NpgsqlDataSource) });

        Assert.NotNull(constructorInfo);
    }

    [Fact]
    public void ImplementsIPostgresTransactionManager()
    {
        Assert.True(typeof(IPostgresTransactionManager).IsAssignableFrom(typeof(PostgresTransactionManager)));
    }

    [Fact]
    public void BeginTransactionAsync_HasExpectedSignature()
    {
        // Verify the method signature exists
        var methodInfo = typeof(PostgresTransactionManager)
            .GetMethod(nameof(PostgresTransactionManager.BeginTransactionAsync));

        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(Task<IPostgresTransactionContext>), methodInfo.ReturnType);
    }

    [Fact]
    public void BeginTransactionAsync_DefaultIsolationLevel_IsReadCommitted()
    {
        // Verify the default parameter value in the method signature
        var methodInfo = typeof(PostgresTransactionManager)
            .GetMethod(nameof(PostgresTransactionManager.BeginTransactionAsync));

        var parameters = methodInfo!.GetParameters();
        Assert.Equal(2, parameters.Length);

        var isolationLevelParam = parameters[0];
        Assert.Equal("isolationLevel", isolationLevelParam.Name);
        Assert.True(isolationLevelParam.HasDefaultValue);
        Assert.Equal(IsolationLevel.ReadCommitted, isolationLevelParam.DefaultValue);
    }
}
