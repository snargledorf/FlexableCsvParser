
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
            //await Tokenize(reader).ConfigureAwait(false);
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

        private static async Task TokenizeAsync(StreamReader reader)
        {
            var config = new TokenizerConfig();
            var tokenizer = new Tokenizer(reader, config);

            Token token;
            while ((token = await tokenizer.ReadTokenAsync().ConfigureAwait(false)).Type != TokenType.EndOfReader)
            {
            }
        }

        private static void Tokenize(StreamReader reader)
        {
            var config = new TokenizerConfig();
            var tokenizer = new Tokenizer(reader, config);

            while (tokenizer.ReadToken().Type != TokenType.EndOfReader)
            {
            }
        }

        private static void Parse(StreamReader reader)
        {
            var config = new CsvParserConfig();
            var parser = new CsvParser(reader, config);

            while (parser.TryReadRecord(out string[] _))
            {
            }
        }
    }
}
