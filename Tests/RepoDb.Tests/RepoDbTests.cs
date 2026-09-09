using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ActDim.Practix.RepoDb;
using ActDim.Practix.RepoDb.Extensions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ActDim.Practix.RepoDb.Tests
{
    public class RepoDbTests
    {
        [Fact]
        public void RepoDbBootstrapper_InitializeSqLite_CanBeCalledIdempotently()
        {
            // Verify multiple calls do not throw
            RepoDbBootstrapper.InitializeSqLite();
            RepoDbBootstrapper.InitializeSqLite();
            RepoDbBootstrapper.InitializeSqLite();
        }

        [Theory]
        [InlineData(null, "%")]
        [InlineData("", "%")]
        [InlineData("   ", "%")]
        [InlineData("test", "test")]
        [InlineData("*test", "%test")]
        [InlineData("test*", "test%")]
        [InlineData("*test*", "%test%")]
        [InlineData("a*b*c", "a%b%c")]
        public void SqlPatternExtensions_NormalizeSqlPattern_ConvertsWildcardsCorrectly(string? input, string expected)
        {
            var result = input.NormalizeSqlPattern();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SqlPatternExtensions_NormalizeSqlPattern_WithCustomDefaultPattern()
        {
            string? input = null;
            var result = input.NormalizeSqlPattern("DEFAULT_PATTERN");
            Assert.Equal("DEFAULT_PATTERN", result);

            var empty = "   ";
            Assert.Equal("DEFAULT_PATTERN", empty.NormalizeSqlPattern("DEFAULT_PATTERN"));
        }

        [Fact]
        public async Task DbConnectionExtensions_ExecuteInTransactionAsync_ThrowsOnNullArguments()
        {
            DbConnection nullConn = null!;
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await nullConn.ExecuteInTransactionAsync(tx => Task.CompletedTask);
            });

            using var conn = new SqliteConnection("Data Source=:memory:");
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await conn.ExecuteInTransactionAsync((Func<DbTransaction, Task>)null!);
            });

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await conn.ExecuteInTransactionAsync((Func<DbTransaction, Task<int>>)null!);
            });
        }

        [Fact]
        public async Task DbConnectionExtensions_ExecuteInTransactionAsync_CommitsOnSuccess()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            // 1. Create table
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT);";
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Execute transactional insert
            await conn.ExecuteInTransactionAsync(async tx =>
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = (SqliteTransaction)tx;
                cmd.CommandText = "INSERT INTO items (id, name) VALUES (1, 'item1');";
                await cmd.ExecuteNonQueryAsync();
            });

            // 3. Verify record was committed
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM items;";
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public async Task DbConnectionExtensions_ExecuteInTransactionAsync_RollsBackOnFailure()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            // 1. Create table
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT);";
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Execute failing transaction
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await conn.ExecuteInTransactionAsync(async tx =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = (SqliteTransaction)tx;
                    cmd.CommandText = "INSERT INTO items (id, name) VALUES (1, 'item1');";
                    await cmd.ExecuteNonQueryAsync();

                    throw new InvalidOperationException("Simulated failure inside transaction");
                });
            });

            // 3. Verify record was rolled back
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM items;";
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public async Task DbConnectionExtensions_ExecuteInTransactionAsync_Generic_ReturnsValueAndOpensConnection()
        {
            // Provide unopened connection to verify it automatically opens
            using var conn = new SqliteConnection("Data Source=:memory:");
            Assert.Equal(ConnectionState.Closed, conn.State);

            var result = await conn.ExecuteInTransactionAsync(async tx =>
            {
                Assert.Equal(ConnectionState.Open, conn.State);
                using var cmd = conn.CreateCommand();
                cmd.Transaction = (SqliteTransaction)tx;
                cmd.CommandText = "SELECT 42;";
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            });

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task DbConnectionExtensions_ExecuteInTransactionAsync_Generic_RollsBackOnFailure()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT);";
                await cmd.ExecuteNonQueryAsync();
            }

            await Assert.ThrowsAsync<ApplicationException>(async () =>
            {
                await conn.ExecuteInTransactionAsync<int>(async tx =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = (SqliteTransaction)tx;
                    cmd.CommandText = "INSERT INTO items (id, name) VALUES (10, 'failed');";
                    await cmd.ExecuteNonQueryAsync();

                    throw new ApplicationException("Generic func failed");
                });
            });

            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM items;";
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                Assert.Equal(0, count);
            }
        }
    }
}
