using BenchmarkDotNet.Running;

namespace FlexableCsvParser.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CsvParserBenchmarks>();
        }
    }
}
