
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using CsvSpanParser;

namespace TestHarness
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            // Testing dataset https://www.kaggle.com/najzeko/steam-reviews-2021
            const string filePath = @"..\..\big.csv";

            using var fs = File.OpenRead(filePath);
            using var dataRateStream = new DataRateStream(fs);
            using var reader = new StreamReader(dataRateStream);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //await ReadLinesAsync(reader).ConfigureAwait(false);
            //await Task.Factory.StartNew(() => ReadLines(reader), TaskCreationOptions.LongRunning).ConfigureAwait(false);
            //await Task.Factory.StartNew(() => Tokenize(reader), TaskCreationOptions.LongRunning).ConfigureAwait(false);
            await Task.Factory.StartNew(() => Parse(reader), TaskCreationOptions.LongRunning).ConfigureAwait(false);

            stopwatch.Stop();

            Console.WriteLine($"Read file in {stopwatch.Elapsed}");
            Console.Write("Press any key to quit...");
            Console.ReadKey(true);
        }

        private static async Task ReadLinesAsync(StreamReader reader)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) != null) ;
        }

        private static void ReadLines(StreamReader reader)
        {
            while (reader.ReadLine() != null) ;
        }

        private static void Tokenize(StreamReader reader)
        {
            var tokenizer = new RFC4180Tokenizer();

            while (tokenizer.NextToken(reader).Type != TokenType.EndOfReader)
            {
            }
        }

        private static void Parse(StreamReader reader)
        {
            var parser = new CsvParser(reader);

            while (parser.TryReadRecord(out string[] _))
            {
            }
        }
    }
}
