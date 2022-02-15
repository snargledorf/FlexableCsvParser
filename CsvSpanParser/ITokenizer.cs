namespace CsvSpanParser
{
    public interface ITokenizer
    {
        IEnumerable<Token> EnumerateTokens(TextReader reader);

        Token NextToken(TextReader reader);
    }
}