using System;

namespace ActDim.Practix.Disposal
{
	/// <summary>
	/// Interface to be implemented by classes that can inform
	/// if they were disposed by calling the Disposed event.
	/// </summary>
	public interface IObservableDisposable:
		IAdvancedDisposable
	{
		/// <summary>
		/// Event invoked when the object is disposed.
		/// It is expected that implementors call the event immediately
		/// if the object is already disposed when a call to add the
		/// handler is made.
		/// </summary>
		event EventHandler Disposed;
	}
}
