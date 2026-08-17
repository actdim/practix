using Ardalis.GuardClauses;
using System;
using System.Linq;
using System.Reflection;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Formatting depth options for introspection member names.
    /// </summary>
    public enum IntrospectionFormatType
    {
        /// <summary>
        /// Compact short formatting.
        /// </summary>
        Compact,

        /// <summary>
        /// Standard formatting.
        /// </summary>
        Normal,

        /// <summary>
        /// Verbose fully-qualified formatting.
        /// </summary>
        Verbose
    }

    /// <summary>
    /// Extension methods for formatting and retrieving <see cref="IntrospectionInfo"/> for reflection members.
    /// </summary>
    public static class IntrospectionExtensions
    {
        private const string FormatSeparator = ".";
        private const string CtorName = "ctor";
        private const string StaticCtorName = "cctor";

        private static bool UseShortName(TypeBaseIntrospectionInfo info)
        {
            Guard.Against.Null(info, nameof(info));
            return info.MemberId.AssemblyFullName.StartsWith("System.") && info.FullName.StartsWith("System.");
        }

        private static string FormatTypeName(TypeBaseIntrospectionInfo info, IntrospectionFormatType formatType, Func<TypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            if (systemTypeFilter == null)
            {
                systemTypeFilter = UseShortName;
            }

            if (formatType == IntrospectionFormatType.Compact || systemTypeFilter(info))
            {
                return info.Name;
            }

            if (formatType == IntrospectionFormatType.Verbose)
            {
                return info.FullName;
            }

            return info.Name;
        }

        private static string FormatMethodName(MethodIntrospectionInfo info, IntrospectionFormatType formatType, Func<TypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            string name;
            if (info.IsConstructor)
            {
                name = info.IsStatic ? StaticCtorName : CtorName;
            }
            else
            {
                name = info.Name;
                if (name == StaticCtorName || name == CtorName)
                {
                    name = $"@{name}";
                }
            }

            return $"{info.DeclaringType.Format(formatType, systemTypeFilter)}{FormatSeparator}{name}";
        }

        /// <summary>
        /// Formats an <see cref="IntrospectionInfo"/> instance into a human-readable string representation based on the format type.
        /// </summary>
        /// <param name="info">The introspection information.</param>
        /// <param name="formatType">The target formatting depth.</param>
        /// <param name="systemTypeFilter">An optional system type filter delegate.</param>
        /// <returns>A formatted member description string.</returns>
        public static string Format(this IntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<TypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            if (info is TypeBaseIntrospectionInfo ti)
            {
                return ti.Format(formatType, systemTypeFilter);
            }

            if (info is MethodIntrospectionInfo mi)
            {
                return mi.Format(formatType, systemTypeFilter);
            }

            if (formatType == IntrospectionFormatType.Compact || info.DeclaringType == null)
            {
                return info.Name;
            }

            return $"{info.DeclaringType.Format(formatType, systemTypeFilter)}{FormatSeparator}{info.Name}";
        }

        private static string Format(this MethodIntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<TypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            var argList = string.Empty;
            var typeArgList = string.Empty;
            if (formatType == IntrospectionFormatType.Compact)
            {
                if (info.Parameters?.Length > 0)
                {
                    argList = "...";
                }

                if (info.IsGeneric)
                {
                    typeArgList = "<...>";
                }
            }
            else
            {
                if (info.Parameters?.Length > 0)
                {
                    argList = string.Join(", ", info.Parameters.Select(x => x.ParameterType.Format(formatType, systemTypeFilter)));
                }

                if (info.IsGenericDefinition && info.GenericParameters != null)
                {
                    typeArgList = $"<{string.Join(", ", info.GenericParameters.Select(x => x.Format(formatType, systemTypeFilter)))}>";
                }
                else if (info.IsGeneric && info.GenericArguments != null)
                {
                    typeArgList = $"<{string.Join(", ", info.GenericArguments.Select(x => x.Format(formatType, systemTypeFilter)))}>";
                }
            }

            var name = FormatMethodName(info, formatType, systemTypeFilter);
            return $"{info.ReturnType.Format(formatType, systemTypeFilter)} {name}{typeArgList}({argList})";
        }

        private static string Format(this TypeBaseIntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<TypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            var typeArgList = string.Empty;
            if (formatType != IntrospectionFormatType.Compact)
            {
                if (info.IsGenericDefinition && info.GenericParameters != null)
                {
                    typeArgList = $"<{string.Join(", ", info.GenericParameters.Select(x => x.Format(IntrospectionFormatType.Compact, systemTypeFilter)))}>";
                }
                else if (info.IsGeneric && info.GenericArguments != null)
                {
                    typeArgList = $"<{string.Join(", ", info.GenericArguments.Select(x => x.Format(IntrospectionFormatType.Compact, systemTypeFilter)))}>";
                }
            }

            var name = FormatTypeName(info, formatType, systemTypeFilter);
            return $"{name}{typeArgList}";
        }

        /// <summary>
        /// Retrieves cached introspection information for a parameter info object.
        /// </summary>
        /// <param name="p">The parameter info.</param>
        /// <param name="fullIntrospection">Whether to build full parameter introspection details.</param>
        /// <returns>A <see cref="ParameterBaseIntrospectionInfo"/> instance.</returns>
        public static ParameterBaseIntrospectionInfo GetIntrospectionInfo(this ParameterInfo p, bool fullIntrospection = false)
        {
            return fullIntrospection
                ? ParameterIntrospectionInfo.Cache.GetValue(p, p => new ParameterIntrospectionInfo(p))
                : ParameterBaseIntrospectionInfo.Cache.GetValue(p, p => new ParameterBaseIntrospectionInfo(p));
        }

        /// <summary>
        /// Retrieves cached introspection information for a member info object.
        /// </summary>
        /// <param name="m">The member info.</param>
        /// <param name="fullIntrospection">Whether to build full member introspection details.</param>
        /// <returns>An <see cref="IntrospectionInfo"/> instance.</returns>
        public static IntrospectionInfo GetIntrospectionInfo(this MemberInfo m, bool fullIntrospection = false)
        {
            return m switch
            {
                Type t => fullIntrospection
                    ? TypeIntrospectionInfo.Cache.GetValue(t, t => new TypeIntrospectionInfo(t))
                    : TypeBaseIntrospectionInfo.Cache.GetValue(t, t => new TypeBaseIntrospectionInfo(t)),
                MethodBase mb => MethodIntrospectionInfo.Cache.GetValue(mb, x => new MethodIntrospectionInfo(x)),
                PropertyInfo p => PropertyIntrospectionInfo.Cache.GetValue(p, x => new PropertyIntrospectionInfo(x)),
                FieldInfo f => FieldIntrospectionInfo.Cache.GetValue(f, x => new FieldIntrospectionInfo(x)),
                _ => IntrospectionInfo.Cache.GetValue(m, x => new IntrospectionInfo(x)),
            };
        }

        /// <summary>
        /// Builds a unique member identifier for the specified member info.
        /// </summary>
        /// <param name="mi">The member info.</param>
        /// <returns>An <see cref="IntrospectionMemberId"/> struct instance.</returns>
        public static IntrospectionMemberId GetIntrospectionMemberId(this MemberInfo mi)
        {
            var t = mi.DeclaringType;
            return new IntrospectionMemberId(t.Assembly.FullName, t.Module.ModuleVersionId, mi.MetadataToken);
        }
    }
}
