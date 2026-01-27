namespace Transactional.PostgreSQL.Tests.Integration
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using Npgsql;
    using Xunit;

    [Trait("Category", "Integration")]
    public class PostgresIntegrationTests : IAsyncLifetime
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        private NpgsqlDataSource? _dataSource;
        private string _tableName = default!;

        public async Task InitializeAsync()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                return;
            }

            _dataSource = NpgsqlDataSource.Create(ConnectionString);
            _tableName = $"test_{Guid.NewGuid():N}";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE TABLE {_tableName} (id SERIAL PRIMARY KEY, name TEXT NOT NULL)";
            await command.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            if (_dataSource != null)
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP TABLE IF EXISTS {_tableName}";
                await command.ExecuteNonQueryAsync();

                await _dataSource.DisposeAsync();
            }
        }

        private void SkipIfNoConnectionString()
        {
            Skip.If(string.IsNullOrEmpty(ConnectionString), "Integration tests require POSTGRES_CONNECTION_STRING environment variable.");
        }

        [SkippableFact]
        public async Task CommitAsync_PersistsDataToDatabase()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync())
            {
                await using var command = context.Transaction.Connection!.CreateCommand();
                command.Transaction = context.Transaction;
                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('test')";
                await command.ExecuteNonQueryAsync();
                await context.CommitAsync();
            }

            await using var countConnection = await _dataSource!.OpenConnectionAsync();
            await using var countCommand = countConnection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM {_tableName}";
            var count = (long)(await countCommand.ExecuteScalarAsync())!;
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public async Task RollbackAsync_DiscardsChanges()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync())
            {
                await using var command = context.Transaction.Connection!.CreateCommand();
                command.Transaction = context.Transaction;
                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('test')";
                await command.ExecuteNonQueryAsync();
                await context.RollbackAsync();
            }

            await using var countConnection = await _dataSource!.OpenConnectionAsync();
            await using var countCommand = countConnection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM {_tableName}";
            var count = (long)(await countCommand.ExecuteScalarAsync())!;
            Assert.Equal(0, count);
        }

        [SkippableFact]
        public async Task DisposeAsync_WithoutCommit_RollsBack()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync())
            {
                await using var command = context.Transaction.Connection!.CreateCommand();
                command.Transaction = context.Transaction;
                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('test')";
                await command.ExecuteNonQueryAsync();
                // Not committing - should rollback on dispose
            }

            await using var countConnection = await _dataSource!.OpenConnectionAsync();
            await using var countCommand = countConnection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM {_tableName}";
            var count = (long)(await countCommand.ExecuteScalarAsync())!;
            Assert.Equal(0, count);
        }

        [SkippableFact]
        public async Task MultipleOperations_InSingleTransaction_SucceedAtomically()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync())
            {
                await using var command = context.Transaction.Connection!.CreateCommand();
                command.Transaction = context.Transaction;

                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('doc1')";
                await command.ExecuteNonQueryAsync();

                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('doc2')";
                await command.ExecuteNonQueryAsync();

                command.CommandText = $"INSERT INTO {_tableName} (name) VALUES ('doc3')";
                await command.ExecuteNonQueryAsync();

                await context.CommitAsync();
            }

            await using var countConnection = await _dataSource!.OpenConnectionAsync();
            await using var countCommand = countConnection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM {_tableName}";
            var count = (long)(await countCommand.ExecuteScalarAsync())!;
            Assert.Equal(3, count);
        }

        [SkippableFact]
        public async Task Transaction_CanBeAccessedFromContext()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync())
            {
                Assert.NotNull(context.Transaction);
                Assert.NotNull(context.Transaction.Connection);

                await context.CommitAsync();
            }
        }

        [SkippableFact]
        public async Task BeginTransactionAsync_WithReadCommittedIsolation_Works()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.Equal(IsolationLevel.ReadCommitted, context.Transaction.IsolationLevel);
                await context.CommitAsync();
            }
        }

        [SkippableFact]
        public async Task BeginTransactionAsync_WithSerializableIsolation_Works()
        {
            SkipIfNoConnectionString();

            var manager = new PostgresTransactionManager(_dataSource!);

            await using (var context = await manager.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                Assert.Equal(IsolationLevel.Serializable, context.Transaction.IsolationLevel);
                await context.CommitAsync();
            }
        }
    }
}
