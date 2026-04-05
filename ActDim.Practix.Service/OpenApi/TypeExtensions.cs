using System.Reflection;
using System.Text;

namespace ActDim.Practix.Service.OpenApi
{
    public static class TypeExtensions
    {
        public static string GetFullTypeName(this Type type, string prefix = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.Append(type.Namespace);
                sb.Append(".");
            }

            var name = GetOwnName(type);
            if (!string.IsNullOrEmpty(prefix))
            {
                name = prefix + name;
            }

            sb.Append(name);

            if (type.IsGenericType)
            {
                sb.Append("<");
                sb.Append(string.Join(", ", type.GetGenericArguments().Select(t => GetFullTypeName(t, null))));
                sb.Append(">");
            }

            return sb.ToString();
        }

        private static string GetOwnName(this Type type)
        {
            string name = type.Name;

            if (type.IsGenericType)
            {
                int backtickIndex = name.IndexOf('`');
                if (backtickIndex >= 0)
                {
                    name = name.Substring(0, backtickIndex);
                }
            }

            if (type.IsNested)
            {
                var declaringType = type.DeclaringType;
                if (declaringType != null)
                {
                    string parentName = GetOwnName(declaringType);
                    name = $"{parentName}+{name}";
                }
            }

            return name;
        }

        private static Type StringType = typeof(string);
        private static Type GenericEnumerableType = typeof(IEnumerable<>);
        private static Type CompatibleDictionaryType = typeof(IDictionary<string, object>);

        public static bool IsOpenApiPrimitive(this Type type)
        {
            if (type.IsPrimitive || type == StringType)
            {
                return true;
            }

            // typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return elementType.IsOpenApiPrimitive();
            }

            var enumerableType = type.GetInterfaces().FirstOrDefault(GenericEnumerableType.Equals);
            if (enumerableType != null)
            {
                return enumerableType.GenericTypeArguments[0].IsOpenApiPrimitive();
            }

            // CompatibleDictionaryType.IsAssignableFrom(type)
            if (CompatibleDictionaryType.Equals(type))
            {
                return true;
            }

            return false;
        }

        private static Assembly[] BaseMvcAssemblies = new[]{
            typeof(Microsoft.AspNetCore.Mvc.IActionResult).Assembly,
            typeof(Microsoft.AspNetCore.Mvc.ActionResult).Assembly
        };

        public static bool IsMvcType(this Type type)
        {
            return BaseMvcAssemblies.Any(assembly => assembly.Equals(type.Assembly));
        }

        /// <summary>
        /// GetSwaggerSchemaId
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetOpenApiSchemaId(this Type type, string schemaPrefix = default, string ownClassPrefix = default)
        {
            if (type.IsOpenApiPrimitive())
            {
                return type.Name;
            }
            var isMvcType = type.IsMvcType();
            var fullName = type.GetFullTypeName(isMvcType ? null : (type.IsClass ? ownClassPrefix : null));
            fullName.Replace("+", ".");
            if (string.IsNullOrEmpty(schemaPrefix))
            {
                return fullName;
            }
            else
            {
                return schemaPrefix + fullName;
            }
        }

        // BaseStructType
        private static readonly Type BaseValueType = typeof(ValueType);
        private static readonly Type BaseObjectType = typeof(object);
        private static readonly IEnumerable<Type> EmptyTypeList = Enumerable.Empty<Type>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetOpenApiSubTypes(Type baseType)
        {
            if (BaseValueType.Equals(baseType) || BaseObjectType.Equals(baseType))
            {
                return EmptyTypeList;
            }
            var assembly = baseType.Assembly;
            var isMvcType = baseType.IsMvcType();
            if (isMvcType
                || assembly.IsDynamic
                || assembly.FullName.StartsWith("Microsoft.")
                || assembly.FullName.StartsWith("System.")
                )
            {
                return EmptyTypeList;
            }

            var types = assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Where(
                    t => !t.IsGenericType || t.GenericTypeArguments.ElementAtOrDefault(0) != default
                );

            return types;
        }
    }
}