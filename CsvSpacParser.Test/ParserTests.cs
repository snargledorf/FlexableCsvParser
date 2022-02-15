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

            var expectedRecord = new[]
            {
                "123",
                "456,\"789\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar";
            var parser = new CsvParser(new StringReader(Csv), new("<Foo", "<FooBar", "<FooB"));

            var expectedRecord = new[]
            {
                "123",
                "456<Foo789",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EmptyQuotedFieldTrailingWhiteSpaceCsv()
        {
            const string Csv = "123, \"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EmptyQuotedFieldCsv()
        {
            const string Csv = "123, \"\",ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180EscapeAtStartOfQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "\"\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void SimpleRFC4180QuotedTextInQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"Bar\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "\"Bar\"",
                "ABC"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
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

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IncompleteRecordDefault()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3));
            parser.TryReadRecord(out string[] _);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IncompleteRecordWithoutSpecifyingRecordLength()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";
            var parser = new CsvParser(new StringReader(Csv));
            
            Assert.IsTrue(parser.TryReadRecord(out string[] _));

            // This one throws the exception
            parser.TryReadRecord(out string[] _);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IncompleteRecordThrowException()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.ThrowException));
            parser.TryReadRecord(out string[] _);
        }

        [TestMethod]
        public void IncompleteRecordFillEmpty()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.FillInWithEmpty));

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                ""
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void IncompleteRecordFillNull()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.FillInWithNull));

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                null
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }

        [TestMethod]
        public void IncompleteRecordTruncate()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.TruncateRecord));

            var expectedRecord = new[]
            {
                "123",
                "Foo"
            };

            Assert.IsTrue(parser.TryReadRecord(out string[] record));
            CollectionAssert.AreEqual(expectedRecord, record);
            Assert.IsFalse(parser.TryReadRecord(out _));
        }
    }
}
