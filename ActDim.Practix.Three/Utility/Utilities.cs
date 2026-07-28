using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using THREE.Serialization;

namespace THREE.Utility
{
    public static class Utilities
    {
        public static T Deserialize<T>(byte[] buffer)
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCaseCustomResolver(),
                Converters = { new BufferAttributeConverter(), new ElementConverter() }
            };

            // var jsonString = Encoding.UTF8.GetString(buffer);
            // return JsonConvert.DeserializeObject<T>(jsonString, settings);

            var jsonSerializer = JsonSerializer.Create(settings);
            // TODO: use MemoryManager            
            using (var stream = new MemoryStream(buffer))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return (T)jsonSerializer.Deserialize(reader, typeof(T));
                }
            }
        }

        public static byte[] Serialize(object obj, bool format = false)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = format ? Formatting.Indented : Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCaseCustomResolver(),
                Converters = { new BufferAttributeConverter(), new ElementConverter() }
            };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, serializerSettings));
        }

        public static IEnumerable<object> OptimizeFloats(IEnumerable<float> floats)
        {
            return floats.Select(f =>
            {
                if (System.Math.Abs(f - System.Math.Floor(f)) <= float.Epsilon)
                {
                    // Convert.ToInt16(f);
                    return Convert.ToInt32(f);
                }
                else
                {
                    return f;
                }
            }).Cast<object>();
        }

        public static IEnumerable<object> Flatten(this IEnumerable<object> source)
        {
            return source.SelectMany(x => x is IEnumerable enumerable ? Flatten(enumerable.Cast<object>()) : new[] { x });
        }

		public static int CombineHashCodes(params object[] objects)
        {
            return CombineHashCodes(objects.Select(obj => ReferenceEquals(obj, null) ? 0 : obj.GetHashCode()));
        }

		public static int CombineHashCodes(params int[] hashCodes)
        {
            return CombineHashCodes(hashCodes);
        }

		public static int CombineHashCodes(IEnumerable<int> hashCodes)
        {
            // int hash1 = (5381 << 16) + 5381;
            // int hash2 = hash1;
            // int i = 0;
            // foreach (var hashCode in hashCodes)
            // {
            // 	if (i % 2 == 0)
            // 	{
            // 		hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
            // 	}
            // 	else
            // 	{
            // 		hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;
            // 	}
            // 	++i;
            // }
            // return hash1 + (hash2 * 1566083941); //unchecked?

            // var result = 0;
            // foreach (var hashCode in hashCodes)
            // {
            // 	if (result == 0)
            // 	{
            // 		result = 17;
            // 		//result = 5381;
            // 	}               
            // 	unchecked
            // 	{
            // 		result = result * 31 + hashCode;
            // 	}
            // 	//result = ((result << 5) + result) ^ hashCode;
            // }
            // return result;

            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            // 7/13/17/23/31/37

            const int b = 378551;
            int a = 63689;
            var result = 0;

            foreach (var hashCode in hashCodes)
            {
                unchecked
                {
                    result = result * a + hashCode;
                    a = a * b;
                }
            }

            return result;
        }
    }
}
