using System;
using ActDim.Practix;
using Xunit;

namespace ActDim.Practix.Common.Tests
{
    public class RandomIdTests
    {
        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(32)]
        public void Generate_Base62_ReturnsExpectedLengthAndCharacters(int length)
        {
            // Arrange
            var alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            // Act
            var result = RandomId.Generate(length); // Default is Base62

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.Contains(c, alphabet);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        public void Generate_Base58_ReturnsCorrectCharacters(int length)
        {
            // Arrange - Bitcoin Base58 alphabet containing 58 characters including uppercase U
            var expectedAlphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            Assert.Equal(58, expectedAlphabet.Length);

            // Act
            var result = RandomId.Generate(length, IdAlphabetType.Base58);

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.Contains(c, expectedAlphabet);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        public void Generate_Base58_DoesNotContainAmbiguousChars(int length)
        {
            // Arrange - Base58 excludes 0, O, I, l
            var ambiguousChars = new[] { '0', 'O', 'I', 'l' };

            // Act
            var result = RandomId.Generate(length, IdAlphabetType.Base58);

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.All(ambiguousChars, forbidden => Assert.DoesNotContain(forbidden, result));
            }
        }

        [Fact]
        public void Generate_Base58_CanIncludeUppercaseU()
        {
            // Act - generate 1000 characters: probability of missing 'U' from Base58 is (57/58)^1000 < 3e-8
            var result = RandomId.Generate(1000, IdAlphabetType.Base58);

            // Assert
            Assert.Contains('U', result);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        public void Generate_CrockfordBase32_ReturnsCorrectCharacters(int length)
        {
            // Arrange - Using the alphabet provided by user: 0123456789ABCDEFGHJKMNPQRSTVWXYZ
            var expectedAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

            // Act
            var result = RandomId.Generate(length, IdAlphabetType.CrockfordBase32);

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.Contains(c, expectedAlphabet);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        public void Generate_CrockfordBase32_DoesNotContainAmbiguousChars(int length)
        {
            // Arrange - Crockford excludes I, L, O, U
            var ambiguousChars = new[] { 'I', 'L', 'O', 'U' };

            // Act
            var result = RandomId.Generate(length, IdAlphabetType.CrockfordBase32);

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.All(ambiguousChars, forbidden => Assert.DoesNotContain(forbidden, result));
            }
        }

        [Fact]
        public void Generate_CustomAlphabet_UsesOnlyProvidedChars()
        {
            // Arrange
            var alphabet = "ABC#$";
            var length = 20;

            // Act
            var result = RandomId.Generate(length, alphabet);

            // Assert
            Assert.Equal(length, result.Length);
            foreach (char c in result)
            {
                Assert.Contains(c, alphabet);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Generate_InvalidLength_ThrowsArgumentOutOfRangeException(int length)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomId.Generate(length));
        }

        [Theory]
        [InlineData("")]
        public void Generate_EmptyCharset_ThrowsArgumentException(string invalidCharset)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => RandomId.Generate(10, invalidCharset));
        }
    }
}
