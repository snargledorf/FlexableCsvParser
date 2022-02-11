using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvSpanParser.Test
{
    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public void SimpleRFC4180Csv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new Tokenizer(new StringReader(Csv), TokenizerConfig.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, "123"),
                Token.FieldDelimiter,
                new Token(TokenType.WhiteSpace, " "),
                Token.Quote,
                new Token(TokenType.Text, "456"),
                Token.FieldDelimiter,
                Token.Escape,
                new Token(TokenType.Text, "789"),
                Token.Escape,
                Token.Quote,
                new Token(TokenType.WhiteSpace, " "),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "ABC"),
                Token.EndOfReader
            };

            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Token token = tokenizer.ReadToken();
                Assert.AreEqual(expectedTokens[i], token);
            }

            Assert.AreEqual(Token.EndOfReader, tokenizer.ReadToken());
        }

        [TestMethod]
        public void SimpleRFC4180CsvFlexable()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new FlexableTokenizer(new StringReader(Csv), TokenizerConfig.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, "123"),
                Token.FieldDelimiter,
                new Token(TokenType.WhiteSpace, " "),
                Token.Quote,
                new Token(TokenType.Text, "456"),
                Token.FieldDelimiter,
                Token.Escape,
                new Token(TokenType.Text, "789"),
                Token.Escape,
                Token.Quote,
                new Token(TokenType.WhiteSpace, " "),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "ABC"),
                Token.EndOfReader
            };

            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Token token = tokenizer.ReadToken();
                Assert.AreEqual(expectedTokens[i], token);
            }

            Assert.AreEqual(Token.EndOfReader, tokenizer.ReadToken());
        }
    }
}