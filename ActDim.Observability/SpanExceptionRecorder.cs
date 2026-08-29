using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ActDim.Observability
{
    /// <summary>
    /// Records an exception on a span at most once per span. The everyday catch / log / rethrow pattern reports the
    /// same exception instance on every layer it passes through, which would otherwise add several identical
    /// <c>exception</c> events - with identical stack traces - to a single span.
    /// </summary>
    /// <remarks>
    /// Keys are held weakly, so an entry lives exactly as long as the exception it belongs to. Identity is preserved
    /// by <c>throw;</c> and across <c>await</c> boundaries (<see cref="System.Runtime.ExceptionServices.ExceptionDispatchInfo"/>
    /// rethrows the same instance), while wrapping into a new exception deliberately produces a separate record.
    /// The set of spans is tracked rather than a single one, so an exception that propagates into another operation is
    /// still recorded on that operation's span.
    /// </remarks>
    internal static class SpanExceptionRecorder
    {
        private static readonly ConditionalWeakTable<Exception, HashSet<ActivitySpanId>> RecordedSpans = new();

        /// <summary>
        /// Records the exception on the activity unless this exact instance has already been recorded there.
        /// </summary>
        /// <returns><c>true</c> when the exception was recorded, <c>false</c> when it was a repeated report.</returns>
        public static bool TryRecordOnce(Activity activity, Exception exception)
        {
            if (activity == null || exception == null)
            {
                return false;
            }

            var recordedSpans = RecordedSpans.GetOrCreateValue(exception);

            lock (recordedSpans)
            {
                if (!recordedSpans.Add(activity.SpanId))
                {
                    return false;
                }
            }

            activity.AddException(exception);
            return true;
        }
    }
}
