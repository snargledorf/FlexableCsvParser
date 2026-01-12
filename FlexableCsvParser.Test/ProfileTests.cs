using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexableCsvParser.Test
{
    [TestClass]
    public class ProfileTests
    {
        private const int RecordCount = 1000;
        private const int FieldCount = 5;
        private string? _csvData;
        private StringReader? _stringReader;
        private CsvParser? _parser;

        public ProfileTests()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < RecordCount; i++)
            {
                for (int j = 0; j < FieldCount; j++)
                {
                    sb.Append($"Field{j}");
                    if (j < FieldCount - 1)
                        sb.Append(",");
                }
                sb.AppendLine();
            }
            _csvData = sb.ToString();
            _stringReader = new StringReader(_csvData!);
            _parser = new CsvParser(_stringReader, FieldCount);
        }

        [TestMethod]
        public void ParseCsv()
        {
            while (_parser!.Read()) ;
        }
    }
}
