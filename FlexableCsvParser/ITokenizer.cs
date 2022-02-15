using System.Collections.Generic;
using System.IO;

namespace FlexableCsvParser
{
    public interface ITokenizer
    {
        IEnumerable<Token> EnumerateTokens(TextReader reader);

        Token NextToken(TextReader reader);
    }
}