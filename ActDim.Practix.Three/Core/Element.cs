using System;
using System.Runtime.Serialization;
using THREE.Utility;

namespace THREE.Core
{
    public interface IElement
    {
		Guid Uuid { get; set; }

		string Name { get; set; }

        // /// <summary>
        // /// 
        // /// </summary>
        // string Type { get; set; }

        // /// <summary>
        // /// 
        // /// </summary>
        // IElement Copy(IElement other);

        // /// <summary>
        // /// 
        // /// </summary>
        // IElement Clone();

        // /// <summary>
        // /// 
        // /// </summary>
        // string ToJSON();
    }

    /// <summary>
    /// Base class for objects which have a Uuid, Name, and Type.
    /// </summary>
    [DataContract]
    public class Element : IElement
    {
        /// <summary>
        /// Unique Guid.
        /// </summary>		
        [DataMember]
        public Guid Uuid { get; set; }

        /// <summary>
        /// Name.
        /// </summary>		
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Type of object.
        /// </summary>		
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Element()
        {
            Uuid = Guid.NewGuid();
            Type = GetType().Name;
        }

        /// <summary>
        /// Convert the object to JSON format. 
        /// </summary>
        /// <returns>A byte[] representation of this object, serialized to JSON.</returns>
        /// <returns>JSON String.</returns>
        public virtual byte[] ToJSON() // bool format
        {
            return Utilities.Serialize(this);
        }
    }
}
