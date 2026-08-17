using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Introspection information DTO model for reflection properties.
    /// </summary>
    public class PropertyIntrospectionInfo : IntrospectionInfo
    {
        internal static new readonly ConditionalWeakTable<PropertyInfo, PropertyIntrospectionInfo> Cache = [];

        /// <summary>Gets or sets property type introspection details.</summary>
        public TypeBaseIntrospectionInfo PropertyType { get; set; }

        /// <summary>Gets or sets whether the property is static.</summary>
        public bool IsStatic { get; set; }

        /// <summary>Gets or sets whether the property accessor is public.</summary>
        public bool IsPublic { get; set; }

        /// <summary>Gets or sets whether the property accessor is private.</summary>
        public bool IsPrivate { get; set; }

        /// <summary>Gets or sets whether the property accessor is protected.</summary>
        public bool IsProtected { get; set; }

        /// <summary>Gets or sets whether the property accessor is internal.</summary>
        public bool IsInternal { get; set; }

        /// <summary>Gets or sets whether the property accessor is protected internal.</summary>
        public bool IsProtectedInternal { get; set; }

        /// <summary>Gets or sets whether the property accessor is private protected.</summary>
        public bool IsPrivateProtected { get; set; }

        /// <summary>Initializes a new instance of the <see cref="PropertyIntrospectionInfo"/> class.</summary>
        public PropertyIntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="PropertyIntrospectionInfo"/> class from a property info.</summary>
        /// <param name="p">The property info.</param>
        public PropertyIntrospectionInfo(PropertyInfo p) : base(p)
        {
            PropertyType = (TypeBaseIntrospectionInfo)p.PropertyType.GetIntrospectionInfo(false);

            var accessor = p.GetMethod ?? p.SetMethod;
            IsStatic = accessor?.IsStatic ?? false;

            if (accessor != null)
            {
                IsPublic = accessor.IsPublic;
                IsPrivate = accessor.IsPrivate;
                IsProtected = accessor.IsFamily;
                IsInternal = accessor.IsAssembly;
                IsProtectedInternal = accessor.IsFamilyOrAssembly;
                IsPrivateProtected = accessor.IsFamilyAndAssembly;
            }
        }
    }
}
