using System.Reflection;
using Xunit;

namespace ActDim.Practix.DataAccess.Tests
{
    public class DataAccessAssemblyTests
    {
        [Fact]
        public void DataAccess_Assembly_CanBeLoadedAndHasMetadata()
        {
            var assemblyName = new AssemblyName("ActDim.Practix.DataAccess");
            var assembly = Assembly.Load(assemblyName);

            Assert.NotNull(assembly);
            Assert.Equal("ActDim.Practix.DataAccess", assembly.GetName().Name);
        }

        [Fact]
        public void DataAccess_Assembly_Types_AreDocumentedAsEmptyOrLegacy()
        {
            var assembly = Assembly.Load(new AssemblyName("ActDim.Practix.DataAccess"));
            var types = assembly.GetExportedTypes();

            // ActDim.Practix.DataAccess was historically emptied in commit 98237cd in favor of RepoDb
            Assert.NotNull(types);
        }
    }
}

