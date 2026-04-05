using ActDim.Practix.Abstractions;
using ActDim.Practix.Abstractions.Introspection;
using NUnit.Framework;
using System;
using ActDim.Practix.Introspection;
using ActDim.Practix.Extensions;

namespace ActDim.Practix.Common.Tests
{
    [TestFixture]
    public class IntrospectionTests
    {
        [Test]
        public IntrospectionTests TestMethod(int arg1, string arg2, IIntrospectionInfo arg3)
        {
            throw new NotImplementedException();
        }

        [Test]
        public void CanCacheIntrospectionInfo()
        {
            var t1 = GetType().GetIntrospectionInfo();

            var t2 = typeof(IntrospectionTests).GetIntrospectionInfo();

            Assert.AreEqual(t1, t2);
        }

        [Test]
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
        }
    }
}