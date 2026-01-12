using BenchmarkDotNet.Running;

namespace FlexableCsvParser.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<CsvParserBenchmarks>();
        }
    }
}
