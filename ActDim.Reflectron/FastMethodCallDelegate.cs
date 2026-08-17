namespace ActDim.Reflectron
{
	/// <summary>
	/// Delegate that represents a dynamic-call to a methodInfo.
	/// It is faster than calling the methodInfo.Invoke.
	/// </summary>
	public delegate object FastMethodCallDelegate(object target, params object[] parameters);
}
