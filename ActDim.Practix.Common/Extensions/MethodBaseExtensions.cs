using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for reflection on <see cref="MethodBase"/>.
    /// </summary>
    public static class MethodBaseExtensions
    {
        /// <summary>
        /// Retrieves the real target method from a compiler-generated async state machine type's <c>MoveNext</c> method.
        /// </summary>
        /// <param name="asyncMethod">The compiler-generated async state machine method info.</param>
        /// <returns>The original method declared on the enclosing class marked with <see cref="AsyncStateMachineAttribute"/>.</returns>
        public static MethodBase GetRealMethodFromAsyncMethod(this MethodBase asyncMethod)
        {
            Guard.Against.Null(asyncMethod, nameof(asyncMethod));
            var generatedType = asyncMethod.DeclaringType;
            var methods = generatedType.DeclaringType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methods.Single(mi => mi.GetCustomAttributes<AsyncStateMachineAttribute>().Any(a => a.StateMachineType == generatedType));
        }
    }
}
