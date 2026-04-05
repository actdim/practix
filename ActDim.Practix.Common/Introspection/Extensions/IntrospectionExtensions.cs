using ActDim.Practix.Abstractions.Introspection;
using Ardalis.GuardClauses;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ActDim.Practix.Introspection
{
    public enum IntrospectionFormatType
    {
        /// <summary>
        /// Short (Default)
        /// </summary>
        Compact,
        Normal,
        /// <summary>
        /// Long
        /// </summary>
        Verbose
    }

    public static class IntrospectionExtensions
    {
        private const string FormatSeparator = ".";

        private static bool UseShortName(ITypeBaseIntrospectionInfo info)
        {
            Guard.Against.Null(info, nameof(info));
            // Assembly.LoadWithPartialName(info.MemberId.AssemblyFullName).GetCustomAttribute<AssemblyCompanyAttribute>()?.Company == "Microsoft Corporation";            
            // System.Collections etc
            return info.MemberId.AssemblyFullName.StartsWith("System.") && info.FullName.StartsWith("System.");
            // Regex.IsMatch(info.MemberId.AssemblyFullName, @"^System\.[\w\.]+", RegexOptions.CultureInvariant);
        }

        private static string FormatTypeName(ITypeBaseIntrospectionInfo info, IntrospectionFormatType formatType, Func<ITypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
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
                // return info.AssemblyQualifiedName;
                return info.FullName;
            }
            // IntrospectionFormatType.Normal
            return info.Name;
        }

        // CLR (ECMA-335, Partition II: Metadata)
        // instance constructor
        private const string CtorName = "ctor";
        // class constructor (type initializer, static constructor).
        private const string StaticCtorName = "cctor";

        private static string FormatMethodName(IMethodIntrospectionInfo info, IntrospectionFormatType formatType, Func<ITypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            string name = null;
            if (info.IsConstructor)
            {
                // name = info.Name.TrimStart('.'); // same
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

        public static string Format(this IIntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<ITypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
        {
            if (info is ITypeBaseIntrospectionInfo ti)
            {
                return ti.Format(formatType, systemTypeFilter);
            }
            if (info is IMethodIntrospectionInfo mi)
            {
                return mi.Format(formatType, systemTypeFilter);
            }
            if (formatType == IntrospectionFormatType.Compact || info.DeclaringType == null)
            {
                return info.Name;
            }
            return $"{info.DeclaringType.Format(formatType, systemTypeFilter)}{FormatSeparator}{info.Name}";
        }

        private static string Format(this IMethodIntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<ITypeBaseIntrospectionInfo, bool> systemTypeFilter = null)
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
                    typeArgList = $"<...>";
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

        private static string Format(this ITypeBaseIntrospectionInfo info, IntrospectionFormatType formatType = IntrospectionFormatType.Compact, Func<ITypeBaseIntrospectionInfo, bool> systemTypeFilter = null) // ToString
        {
            var typeArgList = string.Empty;
            if (formatType != IntrospectionFormatType.Compact)
            {
                if (info.IsGenericDefinition && info.GenericParameters != null)
                {
                    // typeArgList = $"<{string.Join(", ", info.GenericParameters.Select(x => x.Format(formatType, systemTypeFilter)))}>";
                    // typeArgList = $"<{string.Join(", ", info.GenericParameters.Select(x => x.Name))}>";
                    typeArgList = $"<{string.Join(", ", info.GenericParameters.Select(x => x.Format(IntrospectionFormatType.Compact, systemTypeFilter)))}>";
                }
                else if (info.IsGeneric && info.GenericArguments != null)
                {
                    // typeArgList = $"<{string.Join(", ", info.GenericArguments.Select(x => x.Format(formatType, systemTypeFilter)))}>";
                    // typeArgList = $"<{string.Join(", ", info.GenericArguments.Select(x => x.Name))}>";
                    typeArgList = $"<{string.Join(", ", info.GenericArguments.Select(x => x.Format(IntrospectionFormatType.Compact, systemTypeFilter)))}>";
                }
            }
            var name = FormatTypeName(info, formatType, systemTypeFilter);
            return $"{name}{typeArgList}";
        }

        public static IParameterBaseIntrospectionInfo GetIntrospectionInfo(this ParameterInfo p, bool fullIntrospection = false)
        {
            return fullIntrospection ? ParameterIntrospectionInfo.Cache.GetValue(p, p => new ParameterIntrospectionInfo(p))
                : ParameterBaseIntrospectionInfo.Cache.GetValue(p, p => new ParameterBaseIntrospectionInfo(p));
        }
        public static IIntrospectionInfo GetIntrospectionInfo(this MemberInfo m, bool fullIntrospection = false)
        {
            return m switch
            {
                Type t => fullIntrospection ? TypeIntrospectionInfo.Cache.GetValue(t, t => new TypeIntrospectionInfo(t))
                : TypeBaseIntrospectionInfo.Cache.GetValue(t, t => new TypeBaseIntrospectionInfo(t)),
                MethodBase mb => MethodIntrospectionInfo.Cache.GetValue(mb, x => new MethodIntrospectionInfo(x)),
                PropertyInfo p => PropertyIntrospectionInfo.Cache.GetValue(p, x => new PropertyIntrospectionInfo(x)),
                FieldInfo f => FieldIntrospectionInfo.Cache.GetValue(f, x => new FieldIntrospectionInfo(x)),
                _ => IntrospectionInfo.Cache.GetValue(m, x => new IntrospectionInfo(x)),
            };
        }

        public static IntrospectionMemberId GetIntrospectionMemberId(this MemberInfo mi)
        {
            var t = mi.DeclaringType;
            var memberId = new IntrospectionMemberId(t.Assembly.FullName, t.Module.ModuleVersionId, mi.MetadataToken);
            return memberId;
        }
    }
}