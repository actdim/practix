using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using ActDim.Practix.RepoDb.Extensions;
using ActDim.Practix.RepoDb.Sql;
using Microsoft.Data.Sqlite;
using RepoDb.Attributes;
using Xunit;

namespace ActDim.Practix.RepoDb.Tests
{
    public class SqlExpressionTests
    {
        public SqlExpressionTests()
        {
            RepoDbBootstrapper.InitializeSqLite();
        }

        #region Test Entities

        private class CustomerEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private class OrderEntity
        {
            public int OrderId { get; set; }
            public int CustomerId { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        private class OrderItemEntity
        {
            public int ItemId { get; set; }
            public int OrderId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        private class ProductEntity
        {
            public int ProductId { get; set; }
            public string Sku { get; set; } = string.Empty;
        }

        [Map("tbl_users")]
        private class MappedUserEntity
        {
            [Map("usr_id")]
            public int UserId { get; set; }

            [Column("full_name")]
            public string DisplayName { get; set; } = string.Empty;
        }

        private class CustomerOrderDto
        {
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal Total { get; set; }
        }

        #endregion

        [Fact]
        public void DefineSql_SingleTable_GeneratesCorrectSql()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            var cmd = conn.DefineSql<CustomerEntity>(c =>
                $"SELECT {c.Id} AS Id, {c.Name}, {c.Email} FROM {c} WHERE {c.Id} = @id");

            var expected = "SELECT [Id] AS Id, [Name], [Email] FROM [CustomerEntity] WHERE [Id] = @id";
            Assert.Equal(expected, cmd.Sql);
            Assert.Equal(expected, cmd.ToString());

            string asString = cmd;
            Assert.Equal(expected, asString);
        }

        [Fact]
        public void DefineSql_UserRequestedScenario_Select1AsProp_GeneratesCorrectSql()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            var cmd = conn.DefineSql<CustomerEntity>(t => $"Select 1 as {t.Id}");

            var expected = "Select 1 as [Id]";
            Assert.Equal(expected, cmd.Sql);
            Assert.Equal(expected, cmd.ToString());
        }

        [Fact]
        public void DefineSql_TwoTableJoin_GeneratesCorrectSql()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            var cmd = conn.DefineSql<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Id} AS CustomerId, {c.Name}, {o.TotalAmount} AS Total " +
                $"FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId} " +
                $"WHERE {o.Status} = @status");

            var expected = "SELECT [c].[Id] AS CustomerId, [c].[Name], [o].[TotalAmount] AS Total " +
                           "FROM [CustomerEntity] AS [c] JOIN [OrderEntity] AS [o] ON [c].[Id] = [o].[CustomerId] " +
                           "WHERE [o].[Status] = @status";

            Assert.Equal(expected, cmd.Sql);
            Assert.Equal(expected, cmd.ToString());
        }

        [Fact]
        public void DefineSql_RawStringLiteral_MultiLine_GeneratesCorrectSql()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            var cmd = conn.DefineSql<CustomerEntity, OrderEntity>((c, o) => $"""
                SELECT {c.Id} AS CustomerId, {o.TotalAmount}
                FROM {c}
                JOIN {o} ON {c.Id} = {o.CustomerId}
                """);

            Assert.Contains("[c].[Id] AS CustomerId", cmd.Sql);
            Assert.Contains("[CustomerEntity] AS [c]", cmd.Sql);
            Assert.Contains("[OrderEntity] AS [o]", cmd.Sql);
            Assert.Contains("[c].[Id] = [o].[CustomerId]", cmd.Sql);
            Assert.Equal(cmd.Sql, cmd.ToString());
        }

        [Fact]
        public void DefineSql_ThreeAndFourTables_GeneratesCorrectSql()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");

            var cmd3 = conn.DefineSql<CustomerEntity, OrderEntity, OrderItemEntity>((c, o, i) =>
                $"SELECT {c.Name}, {o.OrderId}, {i.ProductName} " +
                $"FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId} JOIN {i} ON {o.OrderId} = {i.OrderId}");

            Assert.Contains("[c].[Name]", cmd3.Sql);
            Assert.Contains("[o].[OrderId]", cmd3.Sql);
            Assert.Contains("[i].[ProductName]", cmd3.Sql);
            Assert.Contains("[CustomerEntity] AS [c]", cmd3.Sql);
            Assert.Contains("[OrderItemEntity] AS [i]", cmd3.Sql);

            var cmd4 = conn.DefineSql<CustomerEntity, OrderEntity, OrderItemEntity, ProductEntity>((c, o, i, p) =>
                $"SELECT {c.Id}, {o.OrderId}, {i.ItemId}, {p.ProductId} FROM {c} JOIN {o} ON 1=1 JOIN {i} ON 1=1 JOIN {p} ON 1=1");

            Assert.Contains("[p].[ProductId]", cmd4.Sql);
            Assert.Contains("[ProductEntity] AS [p]", cmd4.Sql);
        }

        [Fact]
        public void DefineSql_CustomMetadataMappings_AreRespected()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            var cmd = conn.DefineSql<MappedUserEntity>(u =>
                $"SELECT {u.UserId}, {u.DisplayName} FROM {u} WHERE {u.UserId} = 1");

            var expected = "SELECT [usr_id], [full_name] FROM [tbl_users] WHERE [usr_id] = 1";
            Assert.Equal(expected, cmd.Sql);
        }

        [Fact]
        public void SqlTemplate_Standalone_RendersAndBinds()
        {
            var template = SqlTemplate.Define<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Id}, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

            var sqliteRendered = template.Render<SqliteConnection>();
            Assert.Equal(template.ToString(), sqliteRendered);
            Assert.Contains("[c].[Id]", sqliteRendered);
            Assert.Contains("[CustomerEntity] AS [c]", sqliteRendered);

            using var conn = new SqliteConnection("Data Source=:memory:");
            var bound = conn.Bind(template);
            Assert.Equal(sqliteRendered, bound.Sql);
            Assert.Equal(sqliteRendered, bound.ToString());
        }

        [Fact]
        public async Task BoundSql_RealDatabaseExecution_SQLite()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            // 1. Create tables
            using (var schemaCmd = conn.CreateCommand())
            {
                schemaCmd.CommandText = @"
                    CREATE TABLE CustomerEntity (Id INTEGER PRIMARY KEY, Name TEXT, Email TEXT);
                    CREATE TABLE OrderEntity (OrderId INTEGER PRIMARY KEY, CustomerId INTEGER, TotalAmount REAL, Status TEXT);
                ";
                await schemaCmd.ExecuteNonQueryAsync();
            }

            // 2. Insert test data
            using (var insertCmd = conn.CreateCommand())
            {
                insertCmd.CommandText = @"
                    INSERT INTO CustomerEntity (Id, Name, Email) VALUES (1, 'Alice', 'alice@test.com');
                    INSERT INTO CustomerEntity (Id, Name, Email) VALUES (2, 'Bob', 'bob@test.com');
                    INSERT INTO OrderEntity (OrderId, CustomerId, TotalAmount, Status) VALUES (101, 1, 250.50, 'Completed');
                    INSERT INTO OrderEntity (OrderId, CustomerId, TotalAmount, Status) VALUES (102, 1, 49.99, 'Pending');
                    INSERT INTO OrderEntity (OrderId, CustomerId, TotalAmount, Status) VALUES (103, 2, 120.00, 'Completed');
                ";
                await insertCmd.ExecuteNonQueryAsync();
            }

            // 3. ExecuteQueryAsync with DTO materialization
            var query = conn.DefineSql<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Id} AS CustomerId, {c.Name} AS CustomerName, {o.TotalAmount} AS Total " +
                $"FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId} " +
                $"WHERE {o.Status} = @status ORDER BY {c.Id} ASC");

            var results = (await query.ExecuteQueryAsync<CustomerOrderDto>(new { status = "Completed" })).ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal(1, results[0].CustomerId);
            Assert.Equal("Alice", results[0].CustomerName);
            Assert.Equal(250.50m, results[0].Total);

            Assert.Equal(2, results[1].CustomerId);
            Assert.Equal("Bob", results[1].CustomerName);
            Assert.Equal(120.00m, results[1].Total);

            // 4. ExecuteScalarAsync
            var scalarQuery = conn.DefineSql<OrderEntity>(o =>
                $"SELECT COUNT(*) FROM {o} WHERE {o.CustomerId} = @customerId");

            var count = await scalarQuery.ExecuteScalarAsync<int>(new { customerId = 1 });
            Assert.Equal(2, count);

            // 5. ExecuteNonQueryAsync
            var updateCmd = conn.DefineSql<OrderEntity>(o =>
                $"UPDATE {o} SET {o.Status} = @newStatus WHERE {o.OrderId} = @orderId");

            var affected = await updateCmd.ExecuteNonQueryAsync(new { newStatus = "Archived", orderId = 102 });
            Assert.Equal(1, affected);

            // 6. Verify update
            var checkCmd = conn.DefineSql<OrderEntity>(o =>
                $"SELECT {o.Status} FROM {o} WHERE {o.OrderId} = @orderId");

            var newStatus = await checkCmd.ExecuteScalarAsync<string>(new { orderId = 102 });
            Assert.Equal("Archived", newStatus);
        }

        [Fact]
        public void SqlTemplate_Render_WithDifferentDialects()
        {
            var template = SqlTemplate.Define<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Id} AS CustomerId, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

            // 1. PostgreSQL (double quotes)
            var pgSql = template.Render(SqlDialect.PostgreSql);
            Assert.Contains("\"c\".\"Id\" AS CustomerId", pgSql);
            Assert.Contains("\"CustomerEntity\" AS \"c\"", pgSql);
            Assert.Contains("\"OrderEntity\" AS \"o\"", pgSql);
            Assert.Contains("\"c\".\"Id\" = \"o\".\"CustomerId\"", pgSql);

            // 2. MySQL (backticks)
            var mySql = template.Render(SqlDialect.MySql);
            Assert.Contains("`c`.`Id` AS CustomerId", mySql);
            Assert.Contains("`CustomerEntity` AS `c`", mySql);
            Assert.Contains("`OrderEntity` AS `o`", mySql);

            // 3. SQLite / SqlServer / Standard (square brackets)
            var sqliteSql = template.Render(SqlDialect.Sqlite);
            Assert.Contains("[c].[Id] AS CustomerId", sqliteSql);
            Assert.Contains("[CustomerEntity] AS [c]", sqliteSql);

            var stdSql = template.Render(SqlDialect.Standard);
            Assert.Equal(sqliteSql, stdSql);
            Assert.Equal(stdSql, template.ToString());
            Assert.Equal(stdSql, template.Sql);

            string asString = template;
            Assert.Equal(stdSql, asString);
        }

        [Fact]
        public void SqlBuilder_ForDialect_Define_GeneratesExactDialectQuotes()
        {
            // PostgreSQL builder
            var pgTemplate = SqlBuilder.For(SqlDialect.PostgreSql).Define<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Name}, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

            Assert.Equal(SqlDialect.PostgreSql, pgTemplate.Dialect);
            Assert.Contains("\"c\".\"Name\"", pgTemplate.Sql);
            Assert.Contains("\"CustomerEntity\" AS \"c\"", pgTemplate.ToString());

            // MySQL builder
            var mySqlTemplate = SqlBuilder.For(SqlDialect.MySql).Define<CustomerEntity, OrderEntity>((c, o) =>
                $"SELECT {c.Name}, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

            Assert.Equal(SqlDialect.MySql, mySqlTemplate.Dialect);
            Assert.Contains("`c`.`Name`", mySqlTemplate.Sql);
            Assert.Contains("`CustomerEntity` AS `c`", mySqlTemplate.ToString());

            // Static definition shorthand
            var stdTemplate = SqlBuilder.Define<CustomerEntity>(c => $"SELECT {c.Id} FROM {c}");
            Assert.Equal("SELECT [Id] FROM [CustomerEntity]", stdTemplate.Sql);
        }
    }
}
