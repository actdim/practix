using System;

namespace ActDim.Practix.Abstractions.Logging
{
	/// <summary>
	/// Local Context Hierarchical Logger
	/// </summary>
	public interface IScopedLogger: ILogger
	{
		//
		// Summary:
		//     Begins a logical operation scope.
		//
		// Parameters:
		//   state:
		//     The identifier for the scope.
		//
		// Returns:
		//     An IDisposable that ends the logical operation scope on dispose.

		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <param name="state"></param>
		/// <returns></returns>
		IDisposable BeginScope<TState>(TState state, LogLevel logLevel = LogLevel.Information);

        IDisposable BeginScope(LogLevel logLevel = LogLevel.Information);
    }
}
