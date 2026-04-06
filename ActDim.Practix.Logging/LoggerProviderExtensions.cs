using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Logging;
using ActDim.Practix.Abstractions.Messaging;
using ActDim.Practix.Abstractions.Patterns;
using ActDim.Practix.Introspection;
using System.Reflection;

namespace ActDim.Practix.Logging
{
    /// <summary>
    /// ILoggerProvider extension methods for common scenarios.
    /// </summary>
    public static class LoggerProviderExtensions
    {
        private static bool MatchMethodType(MethodBase method, Type type)
        {
            if (method.DeclaringType == type || method.ReflectedType == type)
                return true;
            if (type.BaseType == null)
                return false;
            return MatchMethodType(method, type.BaseType);
        }

        public static LocalLoggerProvider ToLocal<T>(this ILoggerProvider loggerFactory, IProvider<IntrospectionInfo, IntrospectionMemberId> introspectionInfoProvider = null)
        {
            var type = typeof(T);

            return (method, jsonSerializer, callContextProvider) =>
            {
                if (!MatchMethodType(method, type))
                    throw new ArgumentException($"Type \"{type}\" does not contain method \"{method}\"", nameof(method));

                if (!(method is MethodInfo || method is ConstructorInfo))
                    throw new ArgumentException("Invalid method or constructor", nameof(method));

                string categoryName = null;
                var category = method.GetCustomAttributes(typeof(LogCategoryAttribute), false).Cast<LogCategoryAttribute>().FirstOrDefault();
                if (category != null)
                    categoryName = category.Name;

                if (string.IsNullOrEmpty(categoryName))
                {
                    MethodIntrospectionInfo methodIntrospectionInfo = null;
                    if (introspectionInfoProvider != null)
                        methodIntrospectionInfo = (MethodIntrospectionInfo)introspectionInfoProvider.Get(method.GetIntrospectionMemberId());

                    if (methodIntrospectionInfo == null)
                        methodIntrospectionInfo = (MethodIntrospectionInfo)method.GetIntrospectionInfo(false);

                    categoryName = methodIntrospectionInfo.Format();
                }

                return loggerFactory.GetScoped(categoryName);
            };
        }
    }

    /// <summary>
    /// MethodLoggerProvider
    /// </summary>
    public delegate IScopedLogger LocalLoggerProvider(MethodBase method, IJsonSerializer jsonSerializer = null, ICallContextProvider callContextProvider = null);
}
