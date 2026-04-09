using System;

namespace ActDim.Practix.Abstractions.Logging
{
	/// <summary>
	/// Structured Logger
	/// </summary>
	public interface ILogger
	{
		//
		// Summary:
		//     Checks if the given logLevel is enabled.
		//
		// Parameters:
		//   logLevel:
		//     level to be checked.
		//
		// Returns:
		//     true if enabled.
		bool IsEnabled(LogLevel logLevel);
		//
		// Summary:
		//     Writes a log entry.
		//
		// Parameters:
		//   logLevel:
		//     Entry will be written on this level.
		//
		//   eventId:
		//     Id of the event.
		//
		//   state:
		//     The entry to be written. Can be also an object.
		//
		//   exception:
		//     The exception related to this entry.
		//
		//   formatter:
		//     Function to create a string message of the state and exception.

		void Log<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter);
	}
}

