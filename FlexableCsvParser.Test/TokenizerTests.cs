using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexableCsvParser.Test
{
    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public void SimpleRFC4180Csv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new RFC4180Tokenizer();

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

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void SimpleRFC4180CsvQuotedFieldEndOfReader()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new RFC4180Tokenizer();

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
                Token.Quote,
                new Token(TokenType.Text, "ABC"),
                Token.Quote,
                Token.EndOfReader
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void SimpleRFC4180CsvFlexable()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

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

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void SimpleRFC4180CsvFlexableQuotedFieldEndOfReader()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

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
                Token.Quote,
                new Token(TokenType.Text, "ABC"),
                Token.Quote,
                Token.EndOfReader
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar";
            var tokenizer = new FlexableTokenizer(new Delimiters("<Foo", "<FooBar", "<FooB"));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, "123"),
                Token.FieldDelimiter,
                new Token(TokenType.WhiteSpace, " "),
                Token.Quote,
                new Token(TokenType.Text, "456"),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "789"),
                Token.Quote,
                new Token(TokenType.WhiteSpace, " "),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "ABC"),
                Token.EndOfRecord,
                Token.EndOfReader
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BlankQuote()
        {
            new Delimiters(quote: "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BlankFieldDelimiter()
        {
            new Delimiters(fieldDelimiter: "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BlankEndOfRecord()
        {
            new Delimiters(endOfRecord: "");
        }

        [TestMethod]
        public void BlankQuoteAndEscape()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(new(quote: null, escape: null));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, "123"),
                Token.FieldDelimiter,
                new Token(TokenType.WhiteSpace, " "),
                new Token(TokenType.Text, "\"456"),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "\"\"789\"\"\""),
                new Token(TokenType.WhiteSpace, " "),
                Token.FieldDelimiter,
                new Token(TokenType.Text, "\"ABC\""),
                Token.EndOfReader
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void BlankEscape()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(new(escape: null));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, "123"),
                Token.FieldDelimiter,
                new Token(TokenType.WhiteSpace, " "),
                Token.Quote,
                new Token(TokenType.Text, "456"),
                Token.FieldDelimiter,
                Token.Quote,
                Token.Quote,
                new Token(TokenType.Text, "789"),
                Token.Quote,
                Token.Quote,
                Token.Quote,
                new Token(TokenType.WhiteSpace, " "),
                Token.FieldDelimiter,
                Token.Quote,
                new Token(TokenType.Text, "ABC"),
                Token.Quote,
                Token.EndOfReader
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }
    }
}