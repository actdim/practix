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
            Assert.Equal(Guid.Parse("d413f443-1c49-4d84-82fc-054b9e063c21"), BuiltinRoles.Admin);
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
        public void Collection_Properties_CanBeAssigned()
        {
            var id = Guid.NewGuid();
            var collection = new Collection
            {
                Id = id,
                Slug = "core-collection",
                Name = "Core Collection",
                Description = "Core description"
            };

            Assert.Equal(id, collection.Id);
            Assert.Equal("core-collection", collection.Slug);
            Assert.Equal("Core Collection", collection.Name);
            Assert.Equal("Core description", collection.Description);
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
    }
}
