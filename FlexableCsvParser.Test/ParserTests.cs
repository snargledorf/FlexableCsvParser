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

            var expectedRecord = new[]
            {
                "123",
                """
                456 ,"789"
                """,
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task MultipleSharedDelimitersCsvFlexable()
        {
            const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar123<Foo <FooB456<Foo789<FooB <FooABC";

            var expectedRecord = new[]
            {
                "123",
                "456<Foo789",
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length, new CsvParserConfig("<Foo", "<FooBar", "<FooB"));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task SimpleRFC4180EmptyQuotedFieldTrailingWhiteSpaceCsv()
        {
            const string Csv = "123, \"\" ,ABC";

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task SimpleRFC4180EmptyQuotedFieldCsv()
        {
            const string Csv = "123, \"\",ABC";

            var expectedRecord = new[]
            {
                "123",
                "",
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task SimpleRFC4180EscapeAtStartOfQuotedFieldCsv()
        {
            const string Csv = "123, \"\"\"\"\"\" ,ABC";

            var expectedRecord = new[]
            {
                "123",
                "\"\"",
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task SimpleRFC4180QuotedTextInQuotedFieldCsv()
        {
            const string Csv = """"123, """Bar""" ,ABC"""";

            var expectedRecord = new[]
            {
                "123",
                "\"Bar\"",
                "ABC"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task SimpleRFC4180InvalidEscapeAtStartFieldCsv()
        {
            const string Csv = "123, \"\"Bar";
            var parser = new CsvParser(new StringReader(Csv), 2);
            await parser.ReadRecordAsync(default);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteFirstRecordDefaultBehavior()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), 3);
            await parser.ReadRecordAsync(new string[3]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteSecondRecordThrowException()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";
            var parser = new CsvParser(new StringReader(Csv), 3);

            string[] buffer = new string[3];
            var fieldCount = await parser.ReadRecordAsync(buffer);
            Assert.AreEqual(fieldCount, 3);

            // This one throws the exception
            await parser.ReadRecordAsync(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteFirstRecordThrowException()
        {
            const string Csv = "123,Foo";
            var parser = new CsvParser(new StringReader(Csv), 3, new(incompleteRecordHandling: IncompleteRecordHandling.ThrowException));
            await parser.ReadRecordAsync(new string[3]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task IncompleteSecondRecordDefaultBehavior()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";
            var parser = new CsvParser(new StringReader(Csv), 3, new(incompleteRecordHandling: IncompleteRecordHandling.ThrowException));

            string[] buffer = new string[3];
            var fieldCount = await parser.ReadRecordAsync(buffer);
            Assert.AreEqual(fieldCount, 3);

            // This one throws the exception
            await parser.ReadRecordAsync(buffer);
        }

        [TestMethod]
        public async Task IncompleteFirstRecordFillEmpty()
        {
            const string Csv = "123,Foo";

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                ""
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length, new(incompleteRecordHandling: IncompleteRecordHandling.FillInWithEmpty));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task IncompleteSecondRecordFillEmpty()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";

            var expectedRecords = new string[][]
            {
                new string[]
                {
                    "123",
                    "Foo",
                    "Bar"
                },
                new[]
                {
                    "456",
                    "Hello",
                    ""
                }
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecords[0].Length, new(incompleteRecordHandling: IncompleteRecordHandling.FillInWithEmpty));

            var record = new string[expectedRecords[0].Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecords[0], record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecords[1], record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task IncompleteFirstRecordFillNull()
        {
            const string Csv = "123,Foo";

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                null
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length, new(incompleteRecordHandling: IncompleteRecordHandling.FillInWithNull));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task IncompleteSecondRecordFillNull()
        {
            const string Csv = "123,Foo,Bar\r\n456,Hello";

            var expectedRecord = new string?[]
            {
                "123",
                "Foo",
                "Bar"
            };

            var parser = new CsvParser(new StringReader(Csv), expectedRecord.Length, new(incompleteRecordHandling: IncompleteRecordHandling.FillInWithNull));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            expectedRecord = new[]
            {
                "456",
                "Hello",
                null
            };

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(record.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task IncompleteFirstRecordTruncate()
        {
            const string Csv = "123,Foo\r\n456,Bar,Biz";

            var expectedRecord = new[]
            {
                "123",
                "Foo"
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(incompleteRecordHandling: IncompleteRecordHandling.TruncateRecord));

            var record = new string[3];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            expectedRecord = new[]
            {
                "456",
                "Bar",
                "Biz"
            };

            fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task IncompleteSecondRecordTruncate()
        {
            const string Csv = "456,Bar,Biz\r\n123,Foo";

            var expectedRecord = new[]
            {
                "456",
                "Bar",
                "Biz"
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(incompleteRecordHandling: IncompleteRecordHandling.TruncateRecord));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record);

            expectedRecord = new[]
            {
                "123",
                "Foo"
            };

            fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingDefault()
        {
            const string Csv = "123 , Foo ,\" Bar \"";

            var expectedRecord = new[]
            {
                "123 ",
                " Foo ",
                " Bar "
            };

            var parser = new CsvParser(new StringReader(Csv), 3);

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingNone()
        {
            const string Csv = "123 , Foo ,\" Bar \"";

            var expectedRecord = new[]
            {
                "123 ",
                " Foo ",
                " Bar "
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(whiteSpaceTrimming: WhiteSpaceTrimming.None));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingLeading()
        {
            const string Csv = "123 , Foo ,\" Bar \"";

            var expectedRecord = new[]
            {
                "123 ",
                "Foo ",
                "Bar "
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(whiteSpaceTrimming: WhiteSpaceTrimming.Leading));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingTrailing()
        {
            const string Csv = "123 , Foo ,\" Bar \"";

            var expectedRecord = new[]
            {
                "123",
                " Foo",
                " Bar"
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(whiteSpaceTrimming: WhiteSpaceTrimming.Trailing));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }

        [TestMethod]
        public async Task WhiteSpaceTrimmingBoth()
        {
            const string Csv = "123 , Foo ,\" Bar \"";

            var expectedRecord = new[]
            {
                "123",
                "Foo",
                "Bar"
            };

            var parser = new CsvParser(new StringReader(Csv), 3, new(whiteSpaceTrimming: WhiteSpaceTrimming.Both));

            var record = new string[expectedRecord.Length];
            int fieldsRead = await parser.ReadRecordAsync(record).ConfigureAwait(false);
            Assert.AreEqual(expectedRecord.Length, fieldsRead);
            CollectionAssert.AreEqual(expectedRecord, record.AsSpan()[..fieldsRead].ToArray());

            fieldsRead = await parser.ReadRecordAsync(record);
            Assert.AreEqual(0, fieldsRead);
        }
    }
}
