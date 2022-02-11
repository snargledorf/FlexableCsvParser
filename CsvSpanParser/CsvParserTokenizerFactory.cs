namespace CsvSpanParser
{
    internal class CsvParserTokenizerFactory
    {
        internal static ITokenizer GetTokenizerForConfig(TokenizerConfig config, TextReader reader)
        {
            return new FlexableTokenizer(reader, config);
        }
    }
}