using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    /// <summary>
    /// StackTraceExtensions extensions
    /// </summary>
    public static class StackTraceExtensions
    {
        public static MethodBase GetMethod(this StackTrace stackTrace, int index = 1)
        {
            StackFrame sf = stackTrace.GetFrame(index);
            var method = sf.GetMethod();
            if ("Void MoveNext()".Equals(method.ToString()) &&
                method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            {
                // var baseType = value.GetType();
                // var wrappedMethod = st.GetFrames().Skip(1).Select(x => x.GetMethod()).FirstOrDefault(x => x.DeclaringType == baseType);
                // if (wrappedMethod != null)
                // {
                // 	return wrappedMethod;
                // }
                return method.GetRealMethodFromAsyncMethod();
            }
            return method;
        }
    }
}
