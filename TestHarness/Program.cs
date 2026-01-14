
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using FlexableCsvParser;

namespace TestHarness
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Testing dataset https://www.kaggle.com/najzeko/steam-reviews-2021
            const string filePath = @"..\..\..\big.csv";

            using FileStream fs = File.OpenRead(filePath);
            using var dataRateStream = new DataRateStream(fs, TimeSpan.FromSeconds(2));
            dataRateStream.DataRateUpdate += (_, bytesPerSecond) => {
                double kilobytesPerSecond = bytesPerSecond / 1000;
                double megabytesPerSecond = kilobytesPerSecond / 1000;

                Console.Clear();
                Console.Write($"{megabytesPerSecond} Mb/s");
            };

            using var reader = new StreamReader(dataRateStream, Encoding.UTF8);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Parse(reader);

            stopwatch.Stop();

            Console.WriteLine($"Read file in {stopwatch.Elapsed}");
        }

        private static async Task ReadLinesAsync(StreamReader reader)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) != null) ;
        }

        private static void ReadLines(StreamReader reader)
        {
            while (reader.ReadLine() != null) ;
        }

        private static void Parse(StreamReader reader)
        {
            var parser = new CsvParser(reader, 23);
            while (parser.Read())
            {
                for (var i = 0; i < parser.FieldCount; i++)
                    _ = parser.GetString(i);
            }
        }
    }
}
