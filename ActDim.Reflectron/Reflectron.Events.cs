using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly Func<EventInfo, Action<object, Delegate>> GetEventAdderDelegate = GetEventAdder<object, Delegate>;
        private static readonly ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>> EventAdderCache =
            new ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>>(GetEventAdderDelegate);

        private static readonly Func<EventInfo, Action<object, Delegate>> GetEventRemoverDelegate = GetEventRemover<object, Delegate>;
        private static readonly ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>> EventRemoverCache =
            new ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>>(GetEventRemoverDelegate);

        /// <summary>
        /// Gets a delegate to perform fast "event add".
        /// </summary>
        /// <param name="eventInfo">The event info.</param>
        /// <returns>A compiled event adder delegate.</returns>
        public static Action<object, Delegate> GetEventAdder(EventInfo eventInfo)
        {
            Guard.Against.Null(eventInfo, nameof(eventInfo));
            return EventAdderCache.GetOrCreateValue(eventInfo);
        }

        /// <summary>
        /// Gets a strongly-typed delegate to perform fast "event add".
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <typeparam name="TDelegate">The event handler delegate type.</typeparam>
        /// <param name="eventInfo">The event info.</param>
        /// <returns>A compiled event adder delegate.</returns>
        public static Action<TInstance, TDelegate> GetEventAdder<TInstance, TDelegate>(EventInfo eventInfo)
        {
            Guard.Against.Null(eventInfo, nameof(eventInfo));
            return GetEventDelegate<TInstance, TDelegate>(eventInfo.GetAddMethod(), eventInfo.EventHandlerType);
        }

        /// <summary>
        /// Gets a delegate to perform fast "event remove".
        /// </summary>
        /// <param name="eventInfo">The event info.</param>
        /// <returns>A compiled event remover delegate.</returns>
        public static Action<object, Delegate> GetEventRemover(EventInfo eventInfo)
        {
            Guard.Against.Null(eventInfo, nameof(eventInfo));
            return EventRemoverCache.GetOrCreateValue(eventInfo);
        }

        /// <summary>
        /// Gets a strongly-typed delegate to perform fast "event remove".
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <typeparam name="TDelegate">The event handler delegate type.</typeparam>
        /// <param name="eventInfo">The event info.</param>
        /// <returns>A compiled event remover delegate.</returns>
        public static Action<TInstance, TDelegate> GetEventRemover<TInstance, TDelegate>(EventInfo eventInfo)
        {
            Guard.Against.Null(eventInfo, nameof(eventInfo));
            return GetEventDelegate<TInstance, TDelegate>(eventInfo.GetRemoveMethod(), eventInfo.EventHandlerType);
        }

        private static Action<TInstance, TDelegate> GetEventDelegate<TInstance, TDelegate>(MethodInfo methodInfo, Type handlerType)
        {
            var instanceParamExpr = Expression.Parameter(typeof(TInstance), "instance");
            var handlerParamExpr = Expression.Parameter(typeof(TDelegate), "handler");
            Expression readHandlerParamExpr = handlerParamExpr;
            if (handlerType != typeof(TDelegate))
            {
                readHandlerParamExpr = Expression.Convert(handlerParamExpr, handlerType);
            }

            Expression callExpr;
            if (methodInfo.IsStatic)
            {
                callExpr = Expression.Call(methodInfo, readHandlerParamExpr);
            }
            else
            {
                Expression readInstanceParamExpr = instanceParamExpr;
                if (methodInfo.DeclaringType != typeof(TInstance))
                {
                    readInstanceParamExpr = Expression.Convert(instanceParamExpr, methodInfo.DeclaringType);
                }
                callExpr = Expression.Call(readInstanceParamExpr, methodInfo, readHandlerParamExpr);
            }

            var lambdaExpr = Expression.Lambda<Action<TInstance, TDelegate>>(callExpr, instanceParamExpr, handlerParamExpr);
            return lambdaExpr.Compile();
        }
    }
}
