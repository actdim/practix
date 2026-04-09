using System;

namespace ActDim.Practix.Abstractions.Caching
{
	public class InvocationContextConfig
	{
		// public CacheType CacheType { get; set; }

		public string Tag { get; set; }

		public Type InvocationContextSerializerType { get; set; }

		/// <summary>
		/// ExcludeParameterIndexList
		/// </summary>
		public int[] ExcludeParameterIndexes { get; set; }

		/// <summary>
		/// ExcludeParameterTypeList
		/// </summary>
		public Type[] ExcludeParameterTypes { get; set; }
	}
}
