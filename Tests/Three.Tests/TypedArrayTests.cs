using System;
using System.Text.Json;
using Xunit;
using ActDim.Three.Core;
using ActDim.Three.Core.Buffers;
using ActDim.Three.Serialization;

namespace ActDim.Three.Tests
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

		[Fact]
		public void FromStrings_CreatesStringArray()
		{
			var array = TypedArrays.FromStrings(["a", "b", "c"]);

			Assert.Equal(TypedArrays.StringArray, array.Type);
			var typed = Assert.IsType<StringArray>(array);
			Assert.Equal(new[] { "a", "b", "c" }, typed.Data);
		}

		[Fact]
		public void FromArray_BuildsStringArrayForStringType()
		{
			var array = TypedArrays.FromArray(TypedArrays.StringArray, new[] { "a", "b", "c" });

			Assert.Equal(TypedArrays.StringArray, array.Type);
			var typed = Assert.IsType<StringArray>(array);
			Assert.Equal(new[] { "a", "b", "c" }, typed.Data);
		}

		[Fact]
		public void FromArray_TakesOwnershipWhenSourceIsExactType()
		{
			var source = new float[] { 1f, 2f, 3f };

			var typed = Assert.IsType<Float32Array>(TypedArrays.FromArray(TypedArrays.Float32Array, source));

			// Exact-type source is adopted with no copy.
			Assert.Same(source, typed.Data);
		}

		[Fact]
		public void FromArray_ConvertsBoxedNumbers()
		{
			// Heterogeneous boxed object[] (the shape callers actually pass) must convert to the real type.
			object[] source = [0, 1, 0.5, 255];

			var typed = Assert.IsType<Uint8Array>(TypedArrays.FromArray(TypedArrays.Uint8Array, source));

			Assert.Equal(new byte[] { 0, 1, 0, 255 }, typed.Data);
		}

		[Fact]
		public void FromArray_ThrowsOnOutOfRangeValue()
		{
			// Direct Convert-to-target surfaces bad data instead of silently wrapping (300 -> byte).
			object[] source = [300];

			Assert.Throws<OverflowException>(() => TypedArrays.FromArray(TypedArrays.Uint8Array, source));
		}

		[Fact]
		public void StringArray_RoundTripsThroughStj()
		{
			var options = new JsonSerializerOptions();
			options.Converters.Add(new BufferAttributeStjConverter());

			var attribute = BufferAttribute.String(["red", "green", "blue"]);

			var json = JsonSerializer.Serialize(attribute, options);
			var back = JsonSerializer.Deserialize<BufferAttribute>(json, options);

			Assert.Equal(TypedArrays.StringArray, back.Type);
			var typed = Assert.IsType<StringArray>(back.Values);
			Assert.Equal(new[] { "red", "green", "blue" }, typed.Data);
		}

		[Fact]
		public void StringArray_RoundTripsThroughNewtonsoft()
		{
			var converter = new BufferAttributeConverter();

			var attribute = BufferAttribute.String(["red", "green", "blue"]);

			var json = Newtonsoft.Json.JsonConvert.SerializeObject(attribute, converter);
			var back = Newtonsoft.Json.JsonConvert.DeserializeObject<BufferAttribute>(json, converter);

			Assert.Equal(TypedArrays.StringArray, back.Type);
			var typed = Assert.IsType<StringArray>(back.Values);
			Assert.Equal(new[] { "red", "green", "blue" }, typed.Data);
		}
	}
}
