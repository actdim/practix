using System;
using Microsoft.Extensions.Internal;

namespace CanarySystems.Caching // CanarySystems.Misc
{
    public class SystemClock : ISystemClock
    {
        public SystemClock()
        {
        }

        public DateTimeOffset UtcNow {
            get {
                return DateTime.UtcNow;
            }
        }
    }
}
