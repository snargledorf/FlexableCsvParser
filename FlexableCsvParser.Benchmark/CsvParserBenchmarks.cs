using BenchmarkDotNet.Attributes;
using System.Text;

namespace FlexableCsvParser.Benchmark
{
    [MemoryDiagnoser]
    public class CsvParserBenchmarks
    {
        private const int RecordCount = 1000;
        private const int FieldCount = 5;
        private string? _csvData;

        private CsvParserConfig? _config;

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

            _config = new CsvParserConfig();
        }

        [Benchmark]
        public void ParseCsv()
        {
            using var stringReader = new StringReader(_csvData!);
            var parser = new CsvParser(stringReader, FieldCount, _config!);
            while (parser.Read()) ;
        }
    }
}
