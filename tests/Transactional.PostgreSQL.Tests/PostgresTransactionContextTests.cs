namespace Transactional.PostgreSQL.Tests
{
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
    }
}
