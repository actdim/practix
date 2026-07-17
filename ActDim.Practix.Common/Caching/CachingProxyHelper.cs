using System;
using System.Threading.Tasks;

namespace ActDim.Practix.Caching
{
    internal static class CachingProxyHelper
    {
        /// <summary>
        /// Returns the result type of an awaitable (<c>Task&lt;TResult&gt;</c> or
        /// <c>ValueTask&lt;TResult&gt;</c>), or <c>null</c> when <paramref name="type"/> is not one.
        /// </summary>
        public static Type GetAwaitableResultType(Type type, out bool isValueTask)
        {
            isValueTask = false;
            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(Task<>))
                    return type.GetGenericArguments()[0];
                if (def == typeof(ValueTask<>))
                {
                    isValueTask = true;
                    return type.GetGenericArguments()[0];
                }
            }
            return null;
        }
    }
}
