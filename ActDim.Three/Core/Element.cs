using System;
using System.Runtime.Serialization;

namespace ActDim.Three.Core
{
    public interface IElement
    {
		Guid Uuid { get; set; }

		string Name { get; set; }
    }

    /// <summary>
    /// Base class for objects which have a Uuid, Name, and Type.
    /// </summary>
    [DataContract]
    public class Element : IElement
    {
        /// <summary>
        /// Unique Guid. Defaults to <see cref="Guid.Empty"/>; the document layer assigns one during
        /// serialization if still empty.
        /// </summary>
        [DataMember(Name = "uuid")]
        public Guid Uuid { get; set; }

        /// <summary>
        /// Name.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Type of object.
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Element()
        {
            Type = GetType().Name;
        }
    }
}
