using System;
using System.Runtime.Serialization;

namespace THREE.Core
{

    public interface IBufferAttribute : IElement
    {

    }

    [DataContract]
    public class BufferAttribute : IBufferAttribute
    {
        [DataMember]
        public Guid Uuid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int ItemSize { get; set; }

        [DataMember]
        public int Count { get; private set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public bool Normalized { get; set; }

        [DataMember]
        public bool Dynamic { get; set; }

        [DataMember]
        public Array Array { get; set; }

		public BufferAttribute()
        {
            Uuid = Guid.NewGuid();
            Type = GetType().Name;
        }

		public BufferAttribute(string type, Array array, int itemSize, bool normalized) : this()
        {
            Type = type;
            ItemSize = itemSize;
            Array = array;
            Count = Array != null ? Array.Length / ItemSize : 0;
            Normalized = normalized;
            Dynamic = false;
        }

        /// <summary>
        /// BufferArrayType
        /// </summary>
        public class BufferAttributeType
        {
            public const string Int8 = "Int8Array";
            public const string Uint8 = "Uint8Array";
            public const string Uint8Clamped = "Uint8ClampedArray";
            public const string Int16 = "Int16Array";
            public const string Uint16 = "Uint16Array";
            public const string Int32 = "Int32Array";
            public const string Uint32 = "Uint32Array";
            public const string Float32 = "Float32Array";
            public const string Float64 = "Float64Array";
        };
    }
}
