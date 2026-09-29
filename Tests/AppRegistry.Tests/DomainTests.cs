using System;
using ActDim.AppRegistry.Domain.Core;
using ActDim.AppRegistry.Domain.Iam;
using ActDim.AppRegistry.Domain.Registry;
using ActDim.AppRegistry.Domain.Security;
using Xunit;

namespace ActDim.AppRegistry.Tests
{
    public class DomainTests
    {
        private class TestEntityRef : IEntityRef<Guid>
        {
            public string EntityTypeCode { get; set; } = ActDim.AppRegistry.Domain.Core.EntityTypeCode.User;
            public Guid Id { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public string Name { get; set; } = "Test User";
            public string Slug { get; set; } = "test-user";
        }

        [Fact]
        public void EntityRefExtensions_Key_FormatsEntityTypeCodeAndId()
        {
            var entityRef = new TestEntityRef();
            var key = entityRef.Key();
            Assert.Equal("iam:user/11111111-1111-1111-1111-111111111111", key);
        }

        [Fact]
        public void EntityRefExtensions_EntityType_ResolvesKnownType()
        {
            var entityRef = new TestEntityRef { EntityTypeCode = ActDim.AppRegistry.Domain.Core.EntityTypeCode.User };
            var type = entityRef.EntityType();
            Assert.Equal(EntityType.User, type);
        }

        [Fact]
        public void EntityRefExtensions_EntityType_ThrowsOnUnknownCode()
        {
            var entityRef = new TestEntityRef { EntityTypeCode = "unknown_code" };
            Assert.Throws<NotSupportedException>(() => entityRef.EntityType());
        }

        [Fact]
        public void EntityType_NewTypes_ResolveCorrectly()
        {
            Assert.Equal(EntityType.EntityPermission, EntityTypeCode.Map[EntityTypeCode.EntityPermission]);
            Assert.Equal(EntityType.AuditLog, EntityTypeCode.Map[EntityTypeCode.AuditLog]);
        }

        [Fact]
        public void TokenInfo_Properties_CanBeAssignedAndRead()
        {
            var now = DateTimeOffset.UtcNow;
            var token = new TokenInfo
            {
                Token = "sample-token-abc",
                UserId = "user-123",
                Username = "alice",
                ExpiresAt = now.AddHours(1),
                IsUsed = false,
                IsRevoked = false
            };

            Assert.Equal("sample-token-abc", token.Token);
            Assert.Equal("user-123", token.UserId);
            Assert.Equal("alice", token.Username);
            Assert.Equal(now.AddHours(1), token.ExpiresAt);
            Assert.False(token.IsUsed);
            Assert.False(token.IsRevoked);
        }

        [Fact]
        public void BuiltinRoles_Admin_IsDefined()
        {
            Assert.Equal(Guid.Parse("08a31f2e-dfe6-58f5-bd20-3f90d3e5b473"), BuiltinRoles.SuperAdmin);
            Assert.Equal(Guid.Parse("12440c0b-f84f-5207-b3aa-693d9fa87a9b"), BuiltinRoles.Admin);
            Assert.Equal(Guid.Parse("3aff92c6-f64d-58ea-8330-ef496e6f4616"), BuiltinRoles.User);
            Assert.Equal(Guid.Parse("398618f5-ff78-54ac-ae27-1a8734465c46"), BuiltinRoles.Guest);
        }

        [Fact]
        public void User_Properties_WorkAsExpected()
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var user = new User
            {
                Id = id,
                ExternalId = "zitadel-123",
                DisplayName = "John Doe",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                CreatedAt = now,
                UpdatedAt = now
            };

            Assert.Equal(id, user.Id);
            Assert.Equal("zitadel-123", user.ExternalId);
            Assert.Equal("zitadel-123", user.Username);
            Assert.Equal("John Doe", user.Name);
            Assert.Equal("john@example.com", user.Email);
            Assert.Equal("John", user.GivenName);
            Assert.Equal("Doe", user.FamilyName);
            Assert.Equal(ActDim.AppRegistry.Domain.Core.EntityTypeCode.User, user.EntityTypeCode);

            // Verify changing Username changes Slug/ExternalId
            user.Username = "jane-doe";
            Assert.Equal("jane-doe", user.Slug);
        }

        [Fact]
        public void User_EntityTypeCode_ThrowsOnInvalidValue()
        {
            var user = new User();
            Assert.Throws<ArgumentException>(() => user.EntityTypeCode = "invalid");
        }

        [Fact]
        public void Group_Properties_CanBeAssigned()
        {
            var id = Guid.NewGuid();
            var group = new Group
            {
                Id = id,
                Slug = "acme-community",
                Name = "Acme Community"
            };

            Assert.Equal(id, group.Id);
            Assert.Equal("acme-community", group.Slug);
            Assert.Equal("Acme Community", group.Name);
        }

        [Fact]
        public void EntityPermission_Properties_CanBeAssigned()
        {
            var id = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var perm = new EntityPermission
            {
                Id = id,
                RoleId = roleId,
                EntityCode = "iam:user",
                CanCreate = true,
                CanRead = "all",
                CanUpdate = "own",
                CanDelete = "none",
                CanExport = true
            };

            Assert.Equal(id, perm.Id);
            Assert.Equal(roleId, perm.RoleId);
            Assert.Equal("iam:user", perm.EntityCode);
            Assert.True(perm.CanCreate);
            Assert.Equal("all", perm.CanRead);
            Assert.Equal("own", perm.CanUpdate);
            Assert.Equal("none", perm.CanDelete);
            Assert.True(perm.CanExport);
        }

        [Fact]
        public void AuditLog_Properties_CanBeAssigned()
        {
            var id = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var log = new AuditLog
            {
                Id = id,
                EntityCode = "iam:user",
                EntityId = "user-123",
                Action = "UPDATE",
                ActorUserId = actorId,
                OldValues = "{}",
                NewValues = "{\"name\":\"new\"}",
                ChangedFields = new[] { "name" },
                ActorIp = "127.0.0.1",
                CreatedAt = now
            };

            Assert.Equal(id, log.Id);
            Assert.Equal("iam:user", log.EntityCode);
            Assert.Equal("user-123", log.EntityId);
            Assert.Equal("UPDATE", log.Action);
            Assert.Equal(actorId, log.ActorUserId);
            Assert.Equal("{}", log.OldValues);
            Assert.Equal("{\"name\":\"new\"}", log.NewValues);
            Assert.NotNull(log.ChangedFields);
            Assert.Single(log.ChangedFields);
            Assert.Equal("name", log.ChangedFields[0]);
            Assert.Equal("127.0.0.1", log.ActorIp);
            Assert.Equal(now, log.CreatedAt);
        }

        [Fact]
        public void EntityTypeDef_Properties_CanBeAssigned()
        {
            var def = new EntityTypeDef
            {
                Code = "iam:user",
                Name = "User",
                SchemaName = "iam",
                TableName = "users",
                OwnerColumn = "id",
                IsAudited = true,
                Icon = "tabler--user",
                Category = "Identity",
                IsNavVisible = true,
                DefaultSortField = "created_at",
                DefaultSortDir = "desc"
            };

            Assert.Equal("iam:user", def.Code);
            Assert.Equal("User", def.Name);
            Assert.Equal("iam", def.SchemaName);
            Assert.Equal("users", def.TableName);
            Assert.Equal("id", def.OwnerColumn);
            Assert.True(def.IsAudited);
            Assert.Equal("tabler--user", def.Icon);
            Assert.Equal("Identity", def.Category);
            Assert.True(def.IsNavVisible);
            Assert.Equal("created_at", def.DefaultSortField);
            Assert.Equal("desc", def.DefaultSortDir);
        }

        [Fact]
        public void EntityFieldDef_Properties_CanBeAssigned()
        {
            var field = new EntityFieldDef
            {
                EntityTypeCode = "iam:user",
                Name = "email",
                ColumnName = "email",
                DisplayTitle = "Email Address",
                DataType = "character varying(255)",
                ScalarType = "String",
                InputType = "text",
                IsNullable = false,
                IsSearchable = true,
                IsFilterable = true,
                ShowInList = true,
                ShowInForm = true,
                Placeholder = "Enter email",
                HelpText = "User primary email address"
            };

            Assert.Equal("iam:user", field.EntityTypeCode);
            Assert.Equal("email", field.Name);
            Assert.Equal("Email Address", field.DisplayTitle);
            Assert.False(field.IsNullable);
            Assert.True(field.IsSearchable);
            Assert.True(field.IsFilterable);
            Assert.True(field.ShowInList);
            Assert.True(field.ShowInForm);
            Assert.Equal("Enter email", field.Placeholder);
            Assert.Equal("User primary email address", field.HelpText);
        }
    }
}
