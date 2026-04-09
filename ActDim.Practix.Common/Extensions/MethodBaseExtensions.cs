using System;
using System.Reflection;
using System.Linq;
using Ardalis.GuardClauses;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
	/// <summary>
	/// MethodBaseExtensions extensions
	/// </summary>
	public static class MethodBaseExtensions
	{
		public static MethodBase GetRealMethodFromAsyncMethod(this MethodBase asyncMethod)
		{
			Guard.Against.Null(asyncMethod, nameof(asyncMethod));
			var generatedType = asyncMethod.DeclaringType;
			var methods = generatedType.DeclaringType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return methods.Single(mi => mi.GetCustomAttributes<AsyncStateMachineAttribute>().Any(a => a.StateMachineType == generatedType));
		}
	}
}
