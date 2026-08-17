namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Base class for all reflection introspection DTO models.
    /// </summary>
    public class BaseIntrospectionInfo
    {
        /// <summary>Gets or sets the member name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the user-friendly display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets custom user data associated with this introspection model.</summary>
        public object UserData { get; set; }

        /// <summary>Initializes a new instance of the <see cref="BaseIntrospectionInfo"/> class.</summary>
        public BaseIntrospectionInfo()
        {
        }
    }
}
