namespace CsvSpanParser
{
    public interface ITokenizer
    {
        Token ReadToken();

        Task<Token> ReadTokenAsync(CancellationToken cancellationToken = default);
    }
}