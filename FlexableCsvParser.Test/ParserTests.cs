using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexableCsvParser.Test
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public async Task SimpleRFC4180Csv()
        {
            const string Csv = """"
                123, "456 ,""789""" ,ABC
                """";

            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                """
                456 ,"789"
                """,
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync().ConfigureAwait(false);
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord.ToArray(), record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar123<Foo <FooB456<Foo789<FooB <FooABC";
            var parser = new CsvParser(new StringReader(Csv), new("<Foo", "<FooBar", "<FooB"));

            var expectedRecord = new[]
            {
                "123",
                "456<Foo789",
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task SimpleRFC4180EmptyQuotedFieldTrailingWhiteSpaceCsv()
        {
            const string Csv = "123, \"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task SimpleRFC4180EmptyQuotedFieldCsv()
        {
            const string Csv = "123, \"\",ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task SimpleRFC4180EscapeAtStartOfQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "\"\"",
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task SimpleRFC4180QuotedTextInQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"Bar\"\"\" ,ABC";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123",
                "\"Bar\"",
                "ABC"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task SimpleRFC4180InvalidEscapeAtStartFieldCsv()
        {
            const string Csv = "123, \"\"Bar";
            var parser = new CsvParser(new StringReader(Csv));
            await parser.ReadRecordAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteRecordDefault()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3));
            await parser.ReadRecordAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteRecordWithoutSpecifyingRecordLength()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";
            var parser = new CsvParser(new StringReader(Csv));

            var record = await parser.ReadRecordAsync();
            Assert.IsNotNull(record);

            // This one throws the exception
            await parser.ReadRecordAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteRecordThrowException()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.ThrowException));
            await parser.ReadRecordAsync();
        }

        [TestMethod]
        public async Task IncompleteRecordFillEmpty()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.FillInWithEmpty));

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                ""
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task IncompleteRecordFillNull()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.FillInWithNull));

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                null
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task IncompleteRecordTruncate()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), new(recordLength: 3, incompleteRecordHandling: IncompleteRecordHandling.TruncateRecord));

            var expectedRecord = new[]
            {
                "123",
                "Foo"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingDefault()
        {
            const string Csv = "123 , Foo ,\" Bar \"";
            var parser = new CsvParser(new StringReader(Csv));

            var expectedRecord = new[]
            {
                "123 ",
                " Foo ",
                " Bar "
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingNone()
        {
            const string Csv = "123 , Foo ,\" Bar \"";
            var parser = new CsvParser(new StringReader(Csv), new(whiteSpaceTrimming: WhiteSpaceTrimming.None));

            var expectedRecord = new[]
            {
                "123 ",
                " Foo ",
                " Bar "
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingLeading()
        {
            const string Csv = "123 , Foo ,\" Bar \"";
            var parser = new CsvParser(new StringReader(Csv), new(whiteSpaceTrimming: WhiteSpaceTrimming.Leading));

            var expectedRecord = new[]
            {
                "123 ",
                "Foo ",
                "Bar "
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingTrailing()
        {
            const string Csv = "123 , Foo ,\" Bar \"";
            var parser = new CsvParser(new StringReader(Csv), new(whiteSpaceTrimming: WhiteSpaceTrimming.Trailing));

            var expectedRecord = new[]
            {
                "123",
                " Foo",
                " Bar"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingBoth()
        {
            const string Csv = "123 , Foo ,\" Bar \"";
            var parser = new CsvParser(new StringReader(Csv), new(whiteSpaceTrimming: WhiteSpaceTrimming.Both));

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                "Bar"
            };

            (bool success, ReadOnlyMemory<string> record) = await parser.ReadRecordAsync();
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            (success, _) = await parser.ReadRecordAsync();
            Assert.IsFalse(success);
        }
    }
}
