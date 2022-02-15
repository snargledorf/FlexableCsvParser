using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvSpanParser.Test
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void SimpleRFC4180Csv()
        {
            const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecod = new[]
            {
                "123",
                "456,\"789\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar";
            var parser = new CsvParser(new StringReader(Csv), new("<Foo", "<FooBar", "<FooB"));

            var expectedRecod = new[]
            {
                "123",
                "456<Foo789",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EmptyQuotedFieldTrailingWhiteSpaceCsv()
        {
            const string Csv = "123, \"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecod = new[]
            {
                "123",
                "",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EmptyQuotedFieldCsv()
        {
            const string Csv = "123, \"\",ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecod = new[]
            {
                "123",
                "",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EscapeAtStartOfQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecod = new[]
            {
                "123",
                "\"\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180QuotedTextInQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"Bar\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecod = new[]
            {
                "123",
                "\"Bar\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecod, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void SimpleRFC4180InvalidEscapeAtStartFieldCsv()
        {
            const string Csv = "123, \"\"Bar";
            var parser = new CsvParser(new StringReader(Csv));
            parser.TryReadRecord(out string[] _);
        }
    }
}
