using ActDim.Practix.Abstractions.Messaging;

namespace ActDim.Practix.Messaging // ActDim.Practix.CallContext
{
	internal sealed class CallContextProvider : ICallContextProvider
	{
		// private static readonly string DataSlotName = Guid.NewGuid().ToString("N");

		// see also System.Diagnostics.CorrelationManager and System.Runtime.Remoting.Messaging.CallContext
		private readonly AsyncLocal<ICallContext> _contextStack = new();

		private CallContextProvider()
		{

		}

		private static Lazy<CallContextProvider> InternalInstance = new Lazy<CallContextProvider>(() => new CallContextProvider());

		public static CallContextProvider Instance
		{
			get
			{
				return InternalInstance.Value;
			}
		}

		public ICallContext Get()
		{			
			// System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(DataSlotName) as ICallContext
			var value = _contextStack.Value;
			if (value == null) // don't need to lock here
			{
				value = _contextStack.Value = new CallContext();
				// System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(DataSlotName, ...)
			}

			return value;
		}
	}
}