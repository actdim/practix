using System.Runtime.Serialization;

namespace ActDim.Three
{
    /// <summary>
    /// Basic file metadata
    /// </summary>
    /// <remarks>
    /// This is used by scene and camera objects to define which format they are written in.
    /// </remarks>
    [DataContract]
    public class Metadata
    {
        /// <summary>
        /// File version.
        /// </summary>
        [DataMember(Name = "version")]
        public double Version { get; set; }

        /// <summary>
        /// File type.
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; set; }

        /// <summary>
        /// The application which generated this data.
        /// </summary>
        [DataMember(Name = "generator")]
        public string Generator { get; set; }
    }
}
