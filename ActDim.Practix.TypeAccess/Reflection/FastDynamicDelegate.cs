namespace ActDim.Practix.TypeAccess.Reflection
{
	/// <summary>
	/// Delegate that represents a dynamic-call to an untyped delegate.
	/// It is faster than simple calling DynamicInvoke.
	/// </summary>
	public delegate object FastDynamicDelegate(params object[] parameters);
}
