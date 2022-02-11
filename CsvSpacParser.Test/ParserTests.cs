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
            var parser = new CsvParser(new StringReader(Csv), new CsvParserConfig("<Foo", "<FooBar", "<FooB"));

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
    }
}
