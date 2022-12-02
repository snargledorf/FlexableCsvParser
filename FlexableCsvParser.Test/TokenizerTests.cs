using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexableCsvParser.Test
{
    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public async Task SimpleRFC4180Csv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new RFC4180Tokenizer();

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 20, 0, "ABC".AsMemory()),
                //new Token(TokenType.EndOfReader, 23, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task SimpleRFC4180CsvQuotedFieldEndOfReader()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new RFC4180Tokenizer();

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Quote, 20, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 21, 0, "ABC".AsMemory()),
                new Token(TokenType.Quote, 24, 0, Delimiters.RFC4180.Quote.AsMemory()),
                //new Token(TokenType.EndOfReader, 25, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task SimpleRFC4180CsvFlexable()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 20, 0, "ABC".AsMemory()),
                //new Token(TokenType.EndOfReader, 23, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task SimpleRFC4180CsvFlexableQuotedFieldEndOfReader()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Quote, 20, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 21, 0, "ABC".AsMemory()),
                new Token(TokenType.Quote, 24, 0, Delimiters.RFC4180.Quote.AsMemory()),
                //new Token(TokenType.EndOfReader, 25, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar";
            var tokenizer = new FlexableTokenizer(new Delimiters("<Foo", "<FooBar", "<FooB"));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, "<Foo".AsMemory()),
                new Token(TokenType.WhiteSpace, 7, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 8, 0, "<FooB".AsMemory()),
                new Token(TokenType.Text, 13, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 16, 0, "<Foo".AsMemory()),
                new Token(TokenType.Text, 20, 0, "789".AsMemory()),
                new Token(TokenType.Quote, 23, 0, "<FooB".AsMemory()),
                new Token(TokenType.WhiteSpace, 28, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 29, 0, "<Foo".AsMemory()),
                new Token(TokenType.Text, 33, 0, "ABC".AsMemory()),
                new Token(TokenType.EndOfRecord, 36, 0, "<FooBar".AsMemory()),
                //new Token(TokenType.EndOfReader, 0, 1, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task RFC4180EndOfRecordAtEndOfCsv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC\r\n";
            var tokenizer = new RFC4180Tokenizer();

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 20, 0, "ABC".AsMemory()),
                new Token(TokenType.EndOfRecord, 23, 0, Delimiters.RFC4180.EndOfRecord.AsMemory()),
                //new Token(TokenType.EndOfReader, 0, 1, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task FlexableEndOfRecordAtEndOfCsv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC\r\n";
            var tokenizer = new FlexableTokenizer(Delimiters.RFC4180);

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Escape, 10, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Escape, 15, 0, Delimiters.RFC4180.Escape.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 20, 0, "ABC".AsMemory()),
                new Token(TokenType.EndOfRecord, 23, 0, Delimiters.RFC4180.EndOfRecord.AsMemory()),
                //new Token(TokenType.EndOfReader, 0, 1, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
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
        public async Task BlankQuoteAndEscape()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(new(quote: null, escape: null));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Text, 5, 0, "\"456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 10, 0, "\"\"789\"\"\"".AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Text, 20, 0, "\"ABC\"".AsMemory()),
                //new Token(TokenType.EndOfReader, 25, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        [TestMethod]
        public async Task BlankEscape()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,\"ABC\"";
            var tokenizer = new FlexableTokenizer(new(escape: null));

            var expectedTokens = new[]
            {
                new Token(TokenType.Text, 0, 0, "123".AsMemory()),
                new Token(TokenType.FieldDelimiter, 3, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.WhiteSpace, 4, 0, " ".AsMemory()),
                new Token(TokenType.Quote, 5, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 6, 0, "456".AsMemory()),
                new Token(TokenType.FieldDelimiter, 9, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Quote, 10, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Quote, 11, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 12, 0, "789".AsMemory()),
                new Token(TokenType.Quote, 15, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Quote, 16, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Quote, 17, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.WhiteSpace, 18, 0, " ".AsMemory()),
                new Token(TokenType.FieldDelimiter, 19, 0, Delimiters.RFC4180.Field.AsMemory()),
                new Token(TokenType.Quote, 20, 0, Delimiters.RFC4180.Quote.AsMemory()),
                new Token(TokenType.Text, 21, 0, "ABC".AsMemory()),
                new Token(TokenType.Quote, 24, 0, Delimiters.RFC4180.Quote.AsMemory()),
                //new Token(TokenType.EndOfReader, 25, 0, null),
            };

            Token[] tokens = await tokenizer.EnumerateTokensAsync(new StringReader(Csv)).Select(CloneToken).ToArrayAsync();
            CollectionAssert.AreEqual(expectedTokens, tokens);
        }

        private static Token CloneToken(Token t)
        {
            Memory<char> value = new char[t.Value.Length];
            t.Value.CopyTo(value);
            return new Token(t.Type, t.ColumnIndex, t.LineIndex, value);
        }
    }
}