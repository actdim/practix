using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ActDim.Practix.Extensions;
using Xunit;

namespace ActDim.Practix.Common.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Fact]
        public void ToCSharpIdentifier_HandlesNullOrWhiteSpace_ReturnsEmpty()
        {
            Assert.Equal("_empty", ((string)null).ToCSharpIdentifier());
            Assert.Equal("_empty", string.Empty.ToCSharpIdentifier());
            Assert.Equal("_empty", "   ".ToCSharpIdentifier());
        }

        [Fact]
        public void ToCSharpIdentifier_SanitizesSpecialCharacters()
        {
            Assert.Equal("Hello_World", "Hello World".ToCSharpIdentifier());
            Assert.Equal("foo_bar_123", "foo-bar.123".ToCSharpIdentifier());
            Assert.Equal("_123starts_with_digit", "123starts-with-digit".ToCSharpIdentifier());
            Assert.Equal("_valid_name", "_valid_name".ToCSharpIdentifier());
        }

        [Fact]
        public void ToMemory_ReturnsSeekableStreamWithUtf8Content()
        {
            const string content = "Hello, world!";
            using var stream = content.ToMemory();

            Assert.Equal(0L, stream.Position);
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var result = reader.ReadToEnd();
            Assert.Equal(content, result);
        }

        [Fact]
        public async Task ToMemoryAsync_ReturnsSeekableStreamWithCustomEncoding()
        {
            const string content = "Asynchronous pooled stream encoding";
            using var stream = await content.ToMemoryAsync(Encoding.Unicode, TestContext.Current.CancellationToken);

            Assert.Equal(0L, stream.Position);
            using var reader = new StreamReader(stream, Encoding.Unicode, leaveOpen: true);
            var result = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal(content, result);
        }

        [Fact]
        public void SplitQuoted_RespectsDoubleQuoteQualifier()
        {
            var input = """a,b,"c,d",e""";
            var tokens = input.SplitQuoted(",");

            Assert.Equal(4, tokens.Length);
            Assert.Equal("a", tokens[0]);
            Assert.Equal("b", tokens[1]);
            Assert.Equal("\"c,d\"", tokens[2]);
            Assert.Equal("e", tokens[3]);
        }

        [Fact]
        public void SplitQuoted_CustomQualifier_SplitsProperly()
        {
            var input = "a;b;'c;d';e";
            var tokens = input.SplitQuoted(";", "'");

            Assert.Equal(4, tokens.Length);
            Assert.Equal("a", tokens[0]);
            Assert.Equal("b", tokens[1]);
            Assert.Equal("'c;d'", tokens[2]);
            Assert.Equal("e", tokens[3]);
        }

        [Fact]
        public void SplitQuoted_CaseInsensitiveDelimiterMatching()
        {
            var input = "oneANDtwo\"ANDthree\"ANDfour";
            var tokens = input.SplitQuoted("and", "\"", ignoreCase: true);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0]);
            Assert.Equal("two\"ANDthree\"", tokens[1]);
            Assert.Equal("four", tokens[2]);
        }
    }
}
