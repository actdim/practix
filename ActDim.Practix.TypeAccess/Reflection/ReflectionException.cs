
using System;

namespace ActDim.Practix.TypeAccess.Reflection
{
	/// <summary>
	/// Delegate that represents a dynamic-call to an untyped delegate.
	/// It is faster than simple calling DynamicInvoke.
	/// </summary>
	public class ReflectionException: Exception
	{
		//
		// Summary:
		//     Initializes a new instance of the ReflectionException class with a specified error
		//     message.
		//
		// Parameters:
		//   message:
		//     The message that describes the error.
		public ReflectionException(string message): base(message)
		{

		}
	}
}
