using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ardalis.GuardClauses
{
    /// <summary>
    /// Custom guard clauses.
    /// </summary>
    public static class GuardExtensions
    {
        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the stream length does not fit into
        /// <see cref="int"/> (i.e. it cannot be materialized into a single array or string), and returns
        /// the validated length as an <see cref="int"/>.
        /// </summary>
        /// <remarks>
        /// The stream must support <see cref="Stream.Length"/> (be seekable); otherwise reading the length
        /// itself throws.
        /// </remarks>
        public static int LengthWithinInt32(
            this IGuardClause guard,
            Stream stream,
            [CallerArgumentExpression(nameof(stream))] string parameterName = null)
        {
            Guard.Against.Null(stream, parameterName);

            var length = stream.Length;
            if (length > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName, length,
                    $"Stream is too long ({length} bytes); max supported is {int.MaxValue}.");
            }

            return (int)length;
        }
    }
}
