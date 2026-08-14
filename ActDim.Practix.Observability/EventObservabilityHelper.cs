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
        public static Dictionary<string, object> Flatten(object obj, string prefix = "")
        {
            var result = new Dictionary<string, object>();
            if (obj == null)
            {
                return result;
            }

            if (IsSimple(obj))
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    result[prefix] = obj;
                }
                return result;
            }

            if (obj is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    var rawKey = entry.Key?.ToString() ?? string.Empty;
                    var otelKey = ToOtelName(rawKey);
                    var key = string.IsNullOrEmpty(prefix) ? otelKey : $"{prefix}.{otelKey}";
                    result[key] = entry.Value!;
                }
                return result;
            }

            if (obj is IEnumerable enumerable && !(obj is string))
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    var key = $"{prefix}[{i}]";
                    foreach (var kv in Flatten(item, key))
                    {
                        result[kv.Key] = kv.Value!;
                    }
                    i++;
                }
                return result;
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
                    result[key] = value!;
                }
                else
                {
                    foreach (var kv in Flatten(value, key))
                    {
                        result[kv.Key] = kv.Value!;
                    }
                }
            }

            return result;
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
