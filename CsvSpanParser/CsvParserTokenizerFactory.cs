namespace CsvSpanParser
{
    internal static class CsvParserTokenizerFactory
    {
        internal static ITokenizer GetTokenizerForConfig(Delimiters config)
        {
            return new FlexableTokenizer(config);
        }
    }
}