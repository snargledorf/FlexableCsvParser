using BenchmarkDotNet.Attributes;
using System.Text;

namespace FlexableCsvParser.Benchmark
{
    [MemoryDiagnoser]
    public class CsvParserBenchmarks
    {
        private string _csvData;
        private const int RecordCount = 1000;
        private const int FieldCount = 5;

        [GlobalSetup]
        public void Setup()
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
        }

        [Benchmark]
        public void ParseCsv()
        {
            using var reader = new StringReader(_csvData);
            var parser = new CsvParser(reader, FieldCount);
            var buffer = new string[FieldCount];

            while (parser.Read())
                for (int i = 0; i < FieldCount; i++)
                    buffer[i] = parser.GetString(i);
        }
    }
}
