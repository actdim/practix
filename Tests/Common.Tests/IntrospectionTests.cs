using System;
using ActDim.Practix.Common.Introspection;
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
        }
    }
}
