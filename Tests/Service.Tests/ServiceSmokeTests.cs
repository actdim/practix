using System;
using System.Collections.Generic;
using ActDim.Practix.Service.OpenApi;
using Xunit;

namespace ActDim.Practix.Service.Tests
{
    public class ServiceSmokeTests
    {
        [Fact]
        public void CorsPolicies_Constants_AreDefined()
        {
            Assert.Equal("Unrestricted", CorsPolicies.Unrestricted);
            Assert.Equal("FromSettings", CorsPolicies.FromSettings);
        }

        [Fact]
        public void UserInfo_Constructor_SetsProperties()
        {
            var user = new UserInfo("user-123", "alice");
            Assert.Equal("user-123", user.Id);
            Assert.Equal("alice", user.Username);
        }

        [Fact]
        public void RegisteredClaimNames_Constants_AreDefined()
        {
            Assert.Equal("roles", RegisteredClaimNames.Roles);
            Assert.Equal("permissions", RegisteredClaimNames.Permissions);
            Assert.Equal("tid", RegisteredClaimNames.TokenId);
            Assert.Equal("scope", RegisteredClaimNames.Scope);
            Assert.Equal("ver", RegisteredClaimNames.Version);
        }

        [Fact]
        public void TypeExtensions_GetFullTypeName_FormatsSimpleAndGenericTypes()
        {
            var stringName = typeof(string).GetFullTypeName();
            Assert.Equal("System.String", stringName);

            var listName = typeof(List<string>).GetFullTypeName();
            Assert.Equal("System.Collections.Generic.List<System.String>", listName);
        }
    }
}
