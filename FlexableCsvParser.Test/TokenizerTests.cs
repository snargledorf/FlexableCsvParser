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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Text, 20, 0, "ABC"),
                new Token(TokenType.EndOfReader, 23, 0),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Quote, 20, 0),
                new Token(TokenType.Text, 21, 0, "ABC"),
                new Token(TokenType.Quote, 24, 0),
                new Token(TokenType.EndOfReader, 25, 0),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Text, 20, 0, "ABC"),
                new Token(TokenType.EndOfReader, 23, 0),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Quote, 20, 0),
                new Token(TokenType.Text, 21, 0, "ABC"),
                new Token(TokenType.Quote, 24, 0),
                new Token(TokenType.EndOfReader, 25, 0),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 7, 0, " "),
                new Token(TokenType.Quote, 8, 0),
                new Token(TokenType.Text, 13, 0, "456"),
                new Token(TokenType.FieldDelimiter, 16, 0),
                new Token(TokenType.Text, 20, 0, "789"),
                new Token(TokenType.Quote, 23, 0),
                new Token(TokenType.WhiteSpace, 28, 0, " "),
                new Token(TokenType.FieldDelimiter, 29, 0),
                new Token(TokenType.Text, 33, 0, "ABC"),
                new Token(TokenType.EndOfRecord, 36, 0),
                new Token(TokenType.EndOfReader, 0, 1),
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void RFC4180EndOfRecordAtEndOfCsv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC\r\n";
            var tokenizer = new RFC4180Tokenizer();

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Text, 20, 0, "ABC"),
                new Token(TokenType.EndOfRecord, 23, 0),
                new Token(TokenType.EndOfReader, 0, 1),
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public void FlexableEndOfRecordAtEndOfCsv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC\r\n";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Escape, 10, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Escape, 15, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Text, 20, 0, "ABC"),
                new Token(TokenType.EndOfRecord, 23, 0),
                new Token(TokenType.EndOfReader, 0, 1),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Text, 5, 0, "\"456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Text, 10, 0, "\"\"789\"\"\""),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Text, 20, 0, "\"ABC\""),
                new Token(TokenType.EndOfReader, 25, 0),
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
                new Token(TokenType.Text, 0, 0, "123"),
                new Token(TokenType.FieldDelimiter, 3, 0),
                new Token(TokenType.WhiteSpace, 4, 0, " "),
                new Token(TokenType.Quote, 5, 0),
                new Token(TokenType.Text, 6, 0, "456"),
                new Token(TokenType.FieldDelimiter, 9, 0),
                new Token(TokenType.Quote, 10, 0),
                new Token(TokenType.Quote, 11, 0),
                new Token(TokenType.Text, 12, 0, "789"),
                new Token(TokenType.Quote, 15, 0),
                new Token(TokenType.Quote, 16, 0),
                new Token(TokenType.Quote, 17, 0),
                new Token(TokenType.WhiteSpace, 18, 0, " "),
                new Token(TokenType.FieldDelimiter, 19, 0),
                new Token(TokenType.Quote, 20, 0),
                new Token(TokenType.Text, 21, 0, "ABC"),
                new Token(TokenType.Quote, 24, 0),
                new Token(TokenType.EndOfReader, 25, 0),
            };

            Token[] tokens = tokenizer.EnumerateTokens(new StringReader(Csv)).ToArray();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }
    }
}