using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for inspecting stack frames and unwrapping async state machine stack traces on <see cref="StackTrace"/>.
    /// </summary>
    public static class StackTraceExtensions
    {
        /// <summary>
        /// Retrieves the method info associated with the specified stack frame index, resolving real async methods from state machines if present.
        /// </summary>
        /// <param name="stackTrace">The stack trace.</param>
        /// <param name="index">The 0-based frame index (defaults to 1 for immediate caller).</param>
        /// <returns>The <see cref="MethodBase"/> executing at the target frame index.</returns>
        public static MethodBase GetMethod(this StackTrace stackTrace, int index = 1)
        {
            StackFrame sf = stackTrace.GetFrame(index);
            var method = sf.GetMethod();
            if ("Void MoveNext()".Equals(method.ToString()) &&
                method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            {
                return method.GetRealMethodFromAsyncMethod();
            }

            return method;
        }
    }
}
