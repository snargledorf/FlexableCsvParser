using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public interface ITokenizer
    {
        IAsyncEnumerable<Token> EnumerateTokensAsync(TextReader reader);
        bool TryGetNextToken(out Token token);
        ValueTask<bool> ReadAsync(TextReader reader);
    }
}