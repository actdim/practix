using System;
using System.Collections.Generic;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Small numeric helpers that are NOT already provided by the BCL. Pure pass-throughs (Abs, Sqrt, Sign,
    /// Round, Ceiling, Floor, Truncate, trigonometry, Log/Pow/Exp, Min/Max, Clamp, bit-rotation, …) were
    /// removed on purpose - call <see cref="Math"/>, <see cref="MathF"/> or
    /// <see cref="System.Numerics.BitOperations"/> directly instead.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>Returns <c>true</c> when the value has a non-zero fractional part.</summary>
        public static bool HasDecimalPart(this double value)
        {
            return value != Math.Truncate(value);
        }

        /// <summary>Returns <c>true</c> when the value has a non-zero fractional part.</summary>
        public static bool HasDecimalPart(this float value)
        {
            return value != MathF.Truncate(value);
        }

        /// <summary>Returns the value if it is a number, otherwise <paramref name="defaultValue"/> (NaN-coalescing).</summary>
        public static double GetValueOrDefault(this double value, double defaultValue = 0.0)
        {
            return double.IsNaN(value) ? defaultValue : value;
        }

        /// <summary>Returns the value if it is a number, otherwise <paramref name="defaultValue"/> (NaN-coalescing).</summary>
        public static float GetValueOrDefault(this float value, float defaultValue = 0.0f)
        {
            return float.IsNaN(value) ? defaultValue : value;
        }

        /// <summary>Determines whether the value lies inclusively between two bounds (order-independent).</summary>
        public static bool IsBetween(this int value, int a, int b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>Determines whether the value lies inclusively between two bounds (order-independent).</summary>
        public static bool IsBetween(this long value, long a, long b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>Determines whether the value lies inclusively between two bounds (order-independent).</summary>
        public static bool IsBetween(this float value, float a, float b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>Determines whether the value lies inclusively between two bounds (order-independent).</summary>
        public static bool IsBetween(this double value, double a, double b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>Determines whether the value lies inclusively between two bounds (order-independent).</summary>
        public static bool IsBetween(this decimal value, decimal a, decimal b)
        {
            return a < b ? a <= value && value <= b : b <= value && value <= a;
        }

        /// <summary>Rounds the value up to the nearest multiple of <paramref name="factor"/>.</summary>
        public static int RoundUp(this int value, int factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value < 0 ? value - d : value + (factor - d);
        }

        /// <summary>Rounds the value up to the nearest multiple of <paramref name="factor"/>.</summary>
        public static long RoundUp(this long value, long factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value < 0 ? value - d : value + (factor - d);
        }

        /// <summary>Rounds the value up to the nearest multiple of <paramref name="factor"/>.</summary>
        public static float RoundUp(this float value, float factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value < 0 ? value - d : value + (factor - d);
        }

        /// <summary>Rounds the value up to the nearest multiple of <paramref name="factor"/>.</summary>
        public static double RoundUp(this double value, double factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value < 0 ? value - d : value + (factor - d);
        }

        /// <summary>Rounds the value up to the nearest multiple of <paramref name="factor"/>.</summary>
        public static decimal RoundUp(this decimal value, decimal factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value < 0 ? value - d : value + (factor - d);
        }

        /// <summary>Rounds the value down to the nearest multiple of <paramref name="factor"/>.</summary>
        public static int RoundDown(this int value, int factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value > 0 ? value - d : value - (factor + d);
        }

        /// <summary>Rounds the value down to the nearest multiple of <paramref name="factor"/>.</summary>
        public static long RoundDown(this long value, long factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value > 0 ? value - d : value - (factor + d);
        }

        /// <summary>Rounds the value down to the nearest multiple of <paramref name="factor"/>.</summary>
        public static float RoundDown(this float value, float factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value > 0 ? value - d : value - (factor + d);
        }

        /// <summary>Rounds the value down to the nearest multiple of <paramref name="factor"/>.</summary>
        public static double RoundDown(this double value, double factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value > 0 ? value - d : value - (factor + d);
        }

        /// <summary>Rounds the value down to the nearest multiple of <paramref name="factor"/>.</summary>
        public static decimal RoundDown(this decimal value, decimal factor)
        {
            var d = value % factor;
            if (d == 0)
            {
                return value;
            }

            return value > 0 ? value - d : value - (factor + d);
        }

        /// <summary>
        /// Linearly remaps the value from the <c>[fromMin, fromMax]</c> range to <c>[toMin, toMax]</c>
        /// (inverse-lerp then lerp). The input is clamped to the source range first.
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            value = Math.Clamp(value, fromMin, fromMax);

            var normal = (value - fromMin) / (fromMax - fromMin);

            return toMin + (toMax - toMin) * normal;
        }

        /// <summary>
        /// Generates an ascending sequence from <paramref name="start"/> up to (but not including)
        /// <paramref name="bound"/>, stepping by <paramref name="step"/> (must be positive).
        /// </summary>
        public static IEnumerable<int> To(this int start, int bound, int step = 1)
        {
            for (var i = start; i < bound; i += step)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates an ascending sequence from <paramref name="start"/> up to (but not including)
        /// <paramref name="bound"/>, stepping by <paramref name="step"/> (must be positive).
        /// </summary>
        public static IEnumerable<long> To(this long start, long bound, long step = 1)
        {
            for (var i = start; i < bound; i += step)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates an ascending sequence from <paramref name="start"/> up to (but not including)
        /// <paramref name="bound"/>, stepping by <paramref name="step"/> (must be positive).
        /// </summary>
        public static IEnumerable<double> To(this double start, double bound, double step = 1)
        {
            for (var i = start; i < bound; i += step)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates an ascending sequence from <paramref name="start"/> up to (but not including)
        /// <paramref name="bound"/>, stepping by <paramref name="step"/> (must be positive).
        /// </summary>
        public static IEnumerable<decimal> To(this decimal start, decimal bound, decimal step = 1)
        {
            for (var i = start; i < bound; i += step)
            {
                yield return i;
            }
        }
    }
}
