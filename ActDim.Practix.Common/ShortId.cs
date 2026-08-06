using System;
using System.Security.Cryptography;
using Ardalis.GuardClauses;

namespace ActDim.Practix
{
    /// <summary>
    /// Generates short, URL-safe, collision-resistant identifiers.
    /// <para>
    /// Backed by <see cref="RandomNumberGenerator.GetString(ReadOnlySpan{char}, int)"/>, so every id is
    /// cryptographically strong and drawn without modulo bias. The type is stateless and thread-safe;
    /// there is nothing to instantiate or dispose.
    /// </para>
    /// </summary>
    public static class ShortId
    {
        /// <summary>
        /// Default alphabet: digits and letters with the visually ambiguous characters (i, I, Z) removed.
        /// </summary>
        public const string DefaultCharSet = "0123456789abcdefghjlkmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXY";

        /// <summary>
        /// Generates a random identifier of the given length from <see cref="DefaultCharSet"/>.
        /// </summary>
        /// <param name="length">Number of characters to produce; must be positive.</param>
        public static string Generate(int length)
        {
            return Generate(length, DefaultCharSet);
        }

        /// <summary>
        /// Generates a random identifier of the given length from <paramref name="charSet"/>.
        /// </summary>
        /// <param name="length">Number of characters to produce; must be positive.</param>
        /// <param name="charSet">Alphabet to draw from; must not be empty.</param>
        public static string Generate(int length, ReadOnlySpan<char> charSet)
        {
            Guard.Against.NegativeOrZero(length, nameof(length));

            if (charSet.IsEmpty)
            {
                throw new ArgumentException("The character set must not be empty.", nameof(charSet));
            }

            return RandomNumberGenerator.GetString(charSet, length);
        }
    }
}
