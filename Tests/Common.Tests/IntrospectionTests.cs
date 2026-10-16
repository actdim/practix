using System;
using System.Collections.Generic;
using ActDim.Practix.Introspection;
using Xunit;

namespace ActDim.Practix.Common.Tests
{
    public class IntrospectionTests
    {
        // Used as a test subject for reflection in CanFormatIntrospectionInfo
        public IntrospectionTests TestMethod(int arg1, string arg2, IntrospectionInfo arg3)
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void CanCacheIntrospectionInfo()
        {
            var t1 = GetType().GetIntrospectionInfo();
            var t2 = typeof(IntrospectionTests).GetIntrospectionInfo();
            Assert.Equal(t1, t2);
        }

        [Fact]
        public void CanFormatIntrospectionInfo()
        {
            var t = GetType();
            var ti = t.GetIntrospectionInfo();
            var ft1 = ti.Format(IntrospectionFormatType.Compact);
            var ft2 = ti.Format(IntrospectionFormatType.Normal);
            var ft3 = ti.Format(IntrospectionFormatType.Verbose);

            var m = t.GetMethod("TestMethod");
            var mi = m.GetIntrospectionInfo();
            var fm1 = mi.Format(IntrospectionFormatType.Compact);
            var fm2 = mi.Format(IntrospectionFormatType.Normal);
            var fm3 = mi.Format(IntrospectionFormatType.Verbose);

            Assert.NotNull(ft1);
            Assert.NotNull(ft2);
            Assert.NotNull(ft3);
            Assert.NotNull(fm1);
            Assert.NotNull(fm2);
            Assert.NotNull(fm3);
        }

        [Fact]
        public void CanIntrospectOpenGenericTypeWithoutStackOverflow()
        {
            var ti = typeof(List<>).GetIntrospectionInfo() as TypeBaseIntrospectionInfo;
            Assert.NotNull(ti);
            Assert.True(ti.IsGeneric);
            Assert.True(ti.IsGenericDefinition);
            Assert.False(ti.IsGenericParameter);
            Assert.Single(ti.GenericParameters);
            Assert.Empty(ti.GenericArguments);

            var genericParam = ti.GenericParameters[0];
            Assert.Equal("T", genericParam.Name);
            Assert.True(genericParam.IsGenericParameter);
            Assert.Same(ti, genericParam.DeclaringType);
        }

        [Fact]
        public void CanIntrospectMultiParameterOpenGenericType()
        {
            var ti = typeof(Dictionary<,>).GetIntrospectionInfo() as TypeBaseIntrospectionInfo;
            Assert.NotNull(ti);
            Assert.True(ti.IsGeneric);
            Assert.True(ti.IsGenericDefinition);
            Assert.Equal(2, ti.GenericParameters.Length);
            Assert.Empty(ti.GenericArguments);
            Assert.Equal("TKey", ti.GenericParameters[0].Name);
            Assert.Equal("TValue", ti.GenericParameters[1].Name);
            Assert.Same(ti, ti.GenericParameters[0].DeclaringType);
            Assert.Same(ti, ti.GenericParameters[1].DeclaringType);
        }

        [Fact]
        public void CanIntrospectOpenGenericTypeWithFullIntrospection()
        {
            var ti = typeof(List<>).GetIntrospectionInfo(true) as TypeIntrospectionInfo;
            Assert.NotNull(ti);
            Assert.NotEmpty(ti.Methods);
            Assert.NotEmpty(ti.Properties);
            Assert.NotEmpty(ti.Interfaces);
        }

        [Fact]
        public void TopLevelTypesDoNotThrowNullReferenceException()
        {
            var stringId = typeof(string).GetIntrospectionMemberId();
            Assert.NotNull(stringId);
            Assert.False(string.IsNullOrEmpty(stringId.AssemblyFullName));
            Assert.Equal("System.String", stringId.MemberSignature);

            var intId = typeof(int).GetIntrospectionMemberId();
            Assert.NotNull(intId);
            Assert.False(string.IsNullOrEmpty(intId.AssemblyFullName));

            var testClassId = typeof(IntrospectionTests).GetIntrospectionMemberId();
            Assert.NotNull(testClassId);
            Assert.False(string.IsNullOrEmpty(testClassId.AssemblyFullName));
        }

        [Fact]
        public void TopLevelTypeCanBeStoredInInMemoryStorage()
        {
            var storage = new InMemoryIntrospectionStorage();
            var info = storage.GetOrAdd(typeof(string));
            Assert.NotNull(info);
            Assert.Equal("String", info.Name);

            var found = storage.TryGet(typeof(string).GetIntrospectionMemberId(), out var retrieved);
            Assert.True(found);
            Assert.Same(info, retrieved);
        }

        [Fact]
        public void ClosedGenericsDoNotShareStorageKeys()
        {
            var storage = new InMemoryIntrospectionStorage();
            var infoInt = storage.GetOrAdd(typeof(List<int>));
            var infoString = storage.GetOrAdd(typeof(List<string>));

            Assert.NotSame(infoInt, infoString);

            var idInt = typeof(List<int>).GetIntrospectionMemberId();
            var idString = typeof(List<string>).GetIntrospectionMemberId();
            Assert.NotEqual(idInt, idString);

            var retrievedInt = storage.Get(idInt);
            var retrievedString = storage.Get(idString);
            Assert.Same(infoInt, retrievedInt);
            Assert.Same(infoString, retrievedString);

            var typeInt = Assert.IsAssignableFrom<TypeBaseIntrospectionInfo>(retrievedInt);
            var typeString = Assert.IsAssignableFrom<TypeBaseIntrospectionInfo>(retrievedString);

            Assert.Single(typeInt.GenericArguments);
            Assert.Single(typeString.GenericArguments);
            Assert.Equal("Int32", typeInt.GenericArguments[0].Name);
            Assert.Equal("String", typeString.GenericArguments[0].Name);
        }

        [Fact]
        public void ArrayTypesDoNotShareStorageKeys()
        {
            var storage = new InMemoryIntrospectionStorage();
            var idIntArray = typeof(int[]).GetIntrospectionMemberId();
            var idStringArray = typeof(string[]).GetIntrospectionMemberId();

            Assert.NotEqual(idIntArray, idStringArray);

            var infoIntArray = storage.GetOrAdd(typeof(int[]));
            var infoStringArray = storage.GetOrAdd(typeof(string[]));
            Assert.NotSame(infoIntArray, infoStringArray);

            var typeIntArray = Assert.IsAssignableFrom<TypeBaseIntrospectionInfo>(infoIntArray);
            var typeStringArray = Assert.IsAssignableFrom<TypeBaseIntrospectionInfo>(infoStringArray);

            Assert.True(typeIntArray.IsArray);
            Assert.True(typeStringArray.IsArray);
            Assert.Equal("Int32", typeIntArray.ElementType.Name);
            Assert.Equal("String", typeStringArray.ElementType.Name);
        }

        [Fact]
        public void CanFormatOpenAndClosedGenericTypes()
        {
            var tiOpen = typeof(List<>).GetIntrospectionInfo();
            var formattedOpenCompact = tiOpen.Format(IntrospectionFormatType.Compact);
            var formattedOpenNormal = tiOpen.Format(IntrospectionFormatType.Normal);
            var formattedOpenVerbose = tiOpen.Format(IntrospectionFormatType.Verbose);

            Assert.NotNull(formattedOpenCompact);
            Assert.NotNull(formattedOpenNormal);
            Assert.NotNull(formattedOpenVerbose);

            var tiClosed = typeof(List<int>).GetIntrospectionInfo();
            var formattedClosedCompact = tiClosed.Format(IntrospectionFormatType.Compact);
            var formattedClosedNormal = tiClosed.Format(IntrospectionFormatType.Normal);
            var formattedClosedVerbose = tiClosed.Format(IntrospectionFormatType.Verbose);

            Assert.NotNull(formattedClosedCompact);
            Assert.NotNull(formattedClosedNormal);
            Assert.NotNull(formattedClosedVerbose);
        }
    }
}
