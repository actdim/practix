using System;
using Xunit;
using THREE.Core.Buffers;

namespace ThreeLib.Tests
{
	public class TypedArrayMapping
	{
		[Theory]
		[InlineData("Int8Array", typeof(Int8Array), typeof(sbyte[]))]
		[InlineData("Uint8Array", typeof(Uint8Array), typeof(byte[]))]
		[InlineData("Uint8ClampedArray", typeof(Uint8ClampedArray), typeof(byte[]))]
		[InlineData("Int16Array", typeof(Int16Array), typeof(short[]))]
		[InlineData("Uint16Array", typeof(Uint16Array), typeof(ushort[]))]
		[InlineData("Int32Array", typeof(Int32Array), typeof(int[]))]
		[InlineData("Uint32Array", typeof(Uint32Array), typeof(uint[]))]
		[InlineData("Float16Array", typeof(Float16Array), typeof(Half[]))]
		[InlineData("Float32Array", typeof(Float32Array), typeof(float[]))]
		[InlineData("Float64Array", typeof(Float64Array), typeof(double[]))]
		public void FromDoubles_CreatesTypedArrayForEachDiscriminator(string type, Type expected, Type backing)
		{
			var array = TypedArrays.FromDoubles(type, [1, 2, 3]);

			Assert.Equal(type, array.Type);
			Assert.IsType(expected, array);
			Assert.IsType(backing, array.Data);
			Assert.Equal(3, array.Length);
		}

		[Fact]
		public void FromDoubles_ThrowsOnUnknownType()
		{
			Assert.Throws<Newtonsoft.Json.JsonSerializationException>(
				() => TypedArrays.FromDoubles("NopeArray", [1]));
		}
	}
}
