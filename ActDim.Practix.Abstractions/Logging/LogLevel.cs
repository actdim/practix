namespace ActDim.Practix.Abstractions.Logging
{
	public enum LogLevel
	{
		//
		// Summary:
		//     Logs that contain the most detailed messages. These messages may contain sensitive
		//     application data. These messages are disabled by default and should never be
		//     enabled in a production environment.
		Trace = Microsoft.Extensions.Logging.LogLevel.Trace, // Verbose, All
		//
		// Summary:
		//     Logs that are used for interactive investigation during development. These logs
		//     should primarily contain information useful for debugging and have no long-term
		//     value.
		Debug = Microsoft.Extensions.Logging.LogLevel.Debug,
		//
		// Summary:
		//     Logs that track the general flow of the application. These logs should have long-term
		//     value.
		Information = Microsoft.Extensions.Logging.LogLevel.Information, // Info
		//
		// Summary:
		//     Logs that highlight an abnormal or unexpected event in the application flow,
		//     but do not otherwise cause the application execution to stop.
		Warning = Microsoft.Extensions.Logging.LogLevel.Warning, // Notice
		//
		// Summary:
		//     Logs that highlight when the current flow of execution is stopped due to a failure.
		//     These should indicate a failure in the current activity, not an application-wide
		//     failure.
		Error = Microsoft.Extensions.Logging.LogLevel.Error,
		//
		// Summary:
		//     Logs that describe an unrecoverable application or system crash, or a catastrophic
		//     failure that requires immediate attention.
		Critical = Microsoft.Extensions.Logging.LogLevel.Critical, // Failure, Fatal, Severe, Alert, Emergency
		//
		// Summary:
		//     Not used for writing log messages. Specifies that a logging category should not
		//     write any messages.
		None = Microsoft.Extensions.Logging.LogLevel.None // Off
	}
}
