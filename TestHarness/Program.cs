
using System;
using System.IO;
using System.Threading.Tasks;

using CsvSpanParser;

namespace TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Testing dataset https://www.kaggle.com/najzeko/steam-reviews-2021
            const string filePath = @"..\..\big.csv";

            var config = new TokenizerConfig();
            using var fs = File.OpenRead(filePath);
            using var dataRateStream = new DataRateStream(fs);
            using var reader = new StreamReader(dataRateStream);

            //await ReadLinesAsync(reader).ConfigureAwait(false);
            //await Task.Factory.StartNew(() => ReadLines(reader), TaskCreationOptions.LongRunning).ConfigureAwait(false);
            //await Tokenize(config, reader).ConfigureAwait(false);
            await Task.Factory.StartNew(() => Tokenize(config, reader), TaskCreationOptions.LongRunning).ConfigureAwait(false);

            Console.WriteLine();
            Console.Write("Press any key to quit...");
            Console.ReadKey();
        }

        private static async Task ReadLinesAsync(StreamReader reader)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) != null) ;
        }

        private static void ReadLines(StreamReader reader)
        {
            while (reader.ReadLine() != null) ;
        }

        private static async Task TokenizeAsync(TokenizerConfig config, StreamReader reader)
        {
            var tokenizer = new Tokenizer(reader, config);

            Token token;
            while ((token = await tokenizer.ReadTokenAsync().ConfigureAwait(false)).Type != TokenType.EndOfReader)
            {
            }
        }

        private static void Tokenize(TokenizerConfig config, StreamReader reader)
        {
            var tokenizer = new Tokenizer(reader, config);

            while (tokenizer.ReadToken().Type != TokenType.EndOfReader)
            {
            }
        }
    }
}
