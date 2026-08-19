using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ActDim.Observability
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
        /// <summary>
        /// Flattens the object into a dictionary of dotted OpenTelemetry attribute names.
        /// Names that collapse into the same key after normalization are resolved last-write-wins and are therefore
        /// invisible; use <see cref="FlattenPairs"/> when such collisions must stay observable.
        /// </summary>
        public static Dictionary<string, object> Flatten(
            object obj,
            string prefix = "",
            int maxDepth = 3,
            int maxAttributes = 100)
        {
            var result = new Dictionary<string, object>();
            foreach (var pair in FlattenPairs(obj, prefix, maxDepth, maxAttributes))
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        /// <summary>
        /// Streams the flattened object as name/value pairs, preserving duplicates so that the caller can detect
        /// names collapsing into the same OpenTelemetry attribute.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> FlattenPairs(
            object obj,
            string prefix = "",
            int maxDepth = 3,
            int maxAttributes = 100)
        {
            if (obj == null)
            {
                yield break;
            }

            var context = new FlattenContext(maxDepth, maxAttributes);
            foreach (var pair in FlattenPairsInternal(obj, prefix, depth: 0, context))
            {
                yield return pair;
            }
        }

        private sealed class FlattenContext
        {
            public int Count { get; set; }
            public int MaxDepth { get; }
            public int MaxAttributes { get; }
            public HashSet<object> Visited { get; } = new(ReferenceEqualityComparer.Instance);

            public FlattenContext(int maxDepth, int maxAttributes)
            {
                MaxDepth = maxDepth;
                MaxAttributes = maxAttributes;
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> FlattenPairsInternal(
            object obj,
            string prefix,
            int depth,
            FlattenContext context)
        {
            if (obj == null)
            {
                yield break;
            }

            if (IsSimple(obj))
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (context.Count >= context.MaxAttributes)
                    {
                        yield return new KeyValuePair<string, object>(prefix + ".truncated", "<max_attributes_exceeded>");
                        yield break;
                    }

                    context.Count++;
                    yield return new KeyValuePair<string, object>(prefix, obj);
                }
                yield break;
            }

            if (!context.Visited.Add(obj))
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (context.Count < context.MaxAttributes)
                    {
                        context.Count++;
                        yield return new KeyValuePair<string, object>(prefix, "<cycle>");
                    }
                }
                yield break;
            }

            try
            {
                if (depth >= context.MaxDepth)
                {
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        if (context.Count < context.MaxAttributes)
                        {
                            context.Count++;
                            yield return new KeyValuePair<string, object>(prefix, "<max_depth_exceeded>");
                        }
                    }
                    yield break;
                }

                if (obj is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (context.Count >= context.MaxAttributes)
                        {
                            var truncKey = string.IsNullOrEmpty(prefix) ? "items.truncated" : $"{prefix}.truncated";
                            yield return new KeyValuePair<string, object>(truncKey, "<max_attributes_exceeded>");
                            yield break;
                        }

                        var rawKey = entry.Key?.ToString() ?? string.Empty;
                        var otelKey = ToOtelName(rawKey);
                        var key = string.IsNullOrEmpty(prefix) ? otelKey : $"{prefix}.{otelKey}";

                        if (entry.Value == null || IsSimple(entry.Value))
                        {
                            context.Count++;
                            yield return new KeyValuePair<string, object>(key, entry.Value!);
                        }
                        else
                        {
                            foreach (var kv in FlattenPairsInternal(entry.Value, key, depth + 1, context))
                            {
                                yield return kv;
                            }
                        }
                    }
                    yield break;
                }

                if (obj is IEnumerable enumerable && !(obj is string))
                {
                    var effectivePrefix = string.IsNullOrEmpty(prefix) ? "items" : prefix;
                    int i = 0;
                    foreach (var item in enumerable)
                    {
                        if (context.Count >= context.MaxAttributes)
                        {
                            yield return new KeyValuePair<string, object>($"{effectivePrefix}.truncated", "<max_attributes_exceeded>");
                            yield break;
                        }

                        var key = $"{effectivePrefix}[{i}]";
                        if (item == null || IsSimple(item))
                        {
                            context.Count++;
                            yield return new KeyValuePair<string, object>(key, item!);
                        }
                        else
                        {
                            foreach (var kv in FlattenPairsInternal(item, key, depth + 1, context))
                            {
                                yield return kv;
                            }
                        }
                        i++;
                    }
                    yield break;
                }

                foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (context.Count >= context.MaxAttributes)
                    {
                        var truncKey = string.IsNullOrEmpty(prefix) ? "truncated" : $"{prefix}.truncated";
                        yield return new KeyValuePair<string, object>(truncKey, "<max_attributes_exceeded>");
                        yield break;
                    }

                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    object? value = null;
                    string? errorMessage = null;
                    try
                    {
                        value = prop.GetValue(obj);
                    }
                    catch (Exception ex)
                    {
                        var innerEx = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                        errorMessage = $"<error: {innerEx.Message}>";
                    }

                    var otelName = ToOtelName(prop.Name);
                    var key = string.IsNullOrEmpty(prefix) ? otelName : $"{prefix}.{otelName}";

                    if (errorMessage != null)
                    {
                        context.Count++;
                        yield return new KeyValuePair<string, object>(key, errorMessage);
                        continue;
                    }

                    if (value == null || IsSimple(value))
                    {
                        context.Count++;
                        yield return new KeyValuePair<string, object>(key, value!);
                    }
                    else
                    {
                        foreach (var kv in FlattenPairsInternal(value, key, depth + 1, context))
                        {
                            yield return kv;
                        }
                    }
                }
            }
            finally
            {
                context.Visited.Remove(obj);
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
