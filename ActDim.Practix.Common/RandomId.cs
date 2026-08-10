using System;
using System.Linq;
using System.Security.Cryptography;
using Ardalis.GuardClauses;

namespace ActDim.Practix.Common
{
    /// <summary>
    /// Defines predefined character sets for generating identifiers.
    /// </summary>
    public enum IdAlphabetType
    {
        /// <summary>
        /// Base62 alphabet (a-z, A-Z, 0-9). Provides high identifier density
        /// and is suitable for compact URL-safe identifiers.
        /// </summary>
        Base62,

        /// <summary>
        /// Base58 alphabet (Bitcoin/IPFS style). Removes visually ambiguous characters (e.g., 0, O, I, l) for human readability.
        /// </summary>
        Base58,

        /// <summary>
        /// Crockford's Base32 alphabet. Optimized for maximum readability by removing ambiguous characters.
        /// </summary>
        CrockfordBase32
    }

    /// <summary>
    /// Generates random, URL-safe, collision-resistant identifiers using different character sets.
    /// </summary>
    public static class RandomId
    {
        private const string Base62Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTVWXYZabcdefghijkmnopqrstuvwxyz";
        private const string CrockfordBase32Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>
        /// Generates a random identifier of the given length using the Base62 alphabet.
        /// </summary>
        public static string Generate(int length)
        {
            return Generate(length, IdAlphabetType.Base62);
        }

        /// <summary>
        /// Generates a random identifier of the given length using a specific alphabet type.
        /// </summary>
        public static string Generate(int length, IdAlphabetType type)
        {
            string alphabet;

            switch (type)
            {
                case IdAlphabetType.Base62:
                    alphabet = Base62Alphabet;
                    break;
                case IdAlphabetType.Base58:
                    alphabet = Base58Alphabet;
                    break;
                case IdAlphabetType.CrockfordBase32:
                    alphabet = CrockfordBase32Alphabet;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"The alphabet type {type} is not supported.");
            }

            return Generate(length, alphabet);
        }

        /// <summary>
        /// Generates a random identifier of the given length using a custom alphabet.
        /// </summary>
        public static string Generate(int length, string alphabet)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
            ArgumentException.ThrowIfNullOrEmpty(alphabet);

            if (alphabet.Distinct().Count() != alphabet.Length)
            {
                throw new ArgumentException(
                    "Alphabet must contain unique characters.",
                    nameof(alphabet));
            }

            return RandomNumberGenerator.GetString(alphabet, length);
        }
    }
}
