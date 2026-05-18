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
        private readonly string _csvData;
        private readonly CsvParserConfig _config;

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

            _config = new CsvParserConfig();
        }

        [TestMethod]
        public void ParseCsv()
        {
            var stringReader = new StringReader(_csvData);
            var parser = new CsvParser(stringReader, FieldCount, _config);
            while (parser.Read()) ;
        }
    }
}
