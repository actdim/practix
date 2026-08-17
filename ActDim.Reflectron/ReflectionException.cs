using System;

namespace ActDim.Reflectron
{
	/// <summary>
	/// Exception thrown when reflection or dynamic member access fails.
	/// </summary>
	public class ReflectionException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReflectionException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ReflectionException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReflectionException"/> class with a specified error message
		/// and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public ReflectionException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
