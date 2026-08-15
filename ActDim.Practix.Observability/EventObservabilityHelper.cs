#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Helper utilities for recursive object flattening and OpenTelemetry attribute naming conventions.
    /// </summary>
    public static class EventObservabilityHelper
    {
        /// <summary>
        /// Flattens the object into a dictionary of dotted OpenTelemetry attribute names.
        /// Names that collapse into the same key after normalization are resolved last-write-wins and are therefore
        /// invisible; use <see cref="FlattenPairs"/> when such collisions must stay observable.
        /// </summary>
        public static Dictionary<string, object> Flatten(object obj, string prefix = "")
        {
            var result = new Dictionary<string, object>();
            foreach (var pair in FlattenPairs(obj, prefix))
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        /// <summary>
        /// Streams the flattened object as name/value pairs, preserving duplicates so that the caller can detect
        /// names collapsing into the same OpenTelemetry attribute.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> FlattenPairs(object obj, string prefix = "")
        {
            if (obj == null)
            {
                yield break;
            }

            if (IsSimple(obj))
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    yield return new KeyValuePair<string, object>(prefix, obj);
                }
                yield break;
            }

            if (obj is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    var rawKey = entry.Key?.ToString() ?? string.Empty;
                    var otelKey = ToOtelName(rawKey);
                    var key = string.IsNullOrEmpty(prefix) ? otelKey : $"{prefix}.{otelKey}";
                    yield return new KeyValuePair<string, object>(key, entry.Value!);
                }
                yield break;
            }

            if (obj is IEnumerable enumerable && !(obj is string))
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    var key = $"{prefix}[{i}]";
                    foreach (var kv in FlattenPairs(item, key))
                    {
                        yield return kv;
                    }
                    i++;
                }
                yield break;
            }

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                var value = prop.GetValue(obj);
                var otelName = ToOtelName(prop.Name);
                var key = string.IsNullOrEmpty(prefix) ? otelName : $"{prefix}.{otelName}";

                if (value == null || IsSimple(value))
                {
                    yield return new KeyValuePair<string, object>(key, value!);
                }
                else
                {
                    foreach (var kv in FlattenPairs(value, key))
                    {
                        yield return kv;
                    }
                }
            }
        }

        public static bool IsSimple(object value)
        {
            if (value == null)
            {
                return true;
            }

            var type = value.GetType();
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) || type == typeof(Guid) || type.IsEnum;
        }

        public static string ToOtelName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = name.Trim('{', '}');

            // Structured logging destructuring hints ({@value} / {$value}) are not part of the attribute name
            if (name.Length > 1 && (name[0] == '@' || name[0] == '$'))
            {
                name = name.Substring(1);
            }

            var result = new System.Text.StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                if (ch == '.' || ch == '_')
                {
                    result.Append('.');
                    continue;
                }

                if (char.IsUpper(ch))
                {
                    if (i > 0 && name[i - 1] != '.' && name[i - 1] != '_' && !char.IsUpper(name[i - 1]))
                    {
                        result.Append('.');
                    }
                    result.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }
    }
}
