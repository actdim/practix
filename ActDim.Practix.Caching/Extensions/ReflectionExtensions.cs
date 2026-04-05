using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ActDim.Practix.Caching.Extensions
{
    public static class ReflectionExtensions
    {
        public enum FormatType
        {
            /// <summary>
            /// Short (Default)
            /// </summary>
            Compact,
            /// <summary>
            /// 
            /// </summary>
            Normal,
            /// <summary>
            /// 
            /// </summary>
            Full
        }

        public static string GetMetadataKey(this MemberInfo mi)
        {
            // mi.Module.ScopeName - assembly name
            // mi.Module.FullyQualifiedName == mi.Module.Name
            // TODO: implement resolving member info by metadata key
            return $"{mi.MetadataToken}/{mi.Module.Name}/{mi.Module.Assembly.FullName}";
        }

        public static string Format(this MethodInfo mi, FormatType formatType = FormatType.Compact) // ToString
        {
            if (formatType == FormatType.Compact)
            {
                if (mi.IsConstructor)
                {
                    return mi.DeclaringType.Format(formatType);
                }
                else
                {
                    // use '/' instead of '.'?
                    return $"{mi.DeclaringType.Format(formatType)}.{mi.Name}";
                }
            }
            else
            {
                var parameters = mi.GetParameters();
                if (mi.IsConstructor)
                {
                    return ($"{mi.DeclaringType.Format(formatType)}({string.Join(", ", parameters.Select(p => p.ParameterType.Format(formatType)).ToArray())})");
                }
                else
                {
                    var typeArgumentList = "";
                    var genericParameters = mi.GetGenericArguments();
                    if (genericParameters.Length > 0)
                    {
                        typeArgumentList = $"<{string.Join(", ", genericParameters.Select(tm => tm.Format(formatType)).ToArray())}>";
                    }
                    // use '/' instead of '.'?
                    return ($"{mi.ReturnType.Format(formatType)} {mi.DeclaringType.Format(formatType)}.{mi.Name}{typeArgumentList}({string.Join(", ", parameters.Select(p => p.ParameterType.Format(formatType)).ToArray())})");
                }
            }
        }

        public static string Format(this Type type, FormatType formatType = FormatType.Compact) // ToString
        {
            if (formatType == FormatType.Compact)
            {
                return type.Name;
            }
            var typeArgumentList = "";
            var genericParameters = type.GetGenericArguments();
            if (genericParameters.Length > 0)
            {
                typeArgumentList = $"<{string.Join(", ", genericParameters.Select(t => t.Format(formatType)).ToArray())}>";
            }
            var result = $"{(formatType == FormatType.Full ? type.FullName : type.Name)}{typeArgumentList}";

            result = Regex.Replace(result, "[[][[](.*)[]][]]", "");
            // result = result.Replace("+", "");

            return result;
        }
    }
}