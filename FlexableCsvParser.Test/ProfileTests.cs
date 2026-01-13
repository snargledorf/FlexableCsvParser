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
        private readonly CsvParser? _parser;

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
            
            var csvData = sb.ToString();
            var stringReader = new StringReader(csvData!);
            _parser = new CsvParser(stringReader, FieldCount);
        }

        [TestMethod]
        public void ParseCsv()
        {
            while (_parser!.Read()) ;
        }
    }
}
