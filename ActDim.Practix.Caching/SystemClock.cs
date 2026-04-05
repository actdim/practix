using System;
using Microsoft.Extensions.Internal;

namespace ActDim.Practix.Caching // ActDim.Practix.Misc
{
	public class SystemClock : ISystemClock
	{
		public SystemClock()
		{
		}

		public DateTimeOffset UtcNow
		{
			get
			{
				return DateTime.UtcNow;
			}
		}
	}
}
