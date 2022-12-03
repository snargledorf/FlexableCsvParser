using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public interface ITokenizer
    {
        IEnumerable<ITokenizer> EnumerateTokens(TextReader reader);
        bool TryGetNextToken(TextReader reader);

        TokenType TokenType { get; }
        ReadOnlySpan<char> TokenValue { get; }
        int TokenLineNumber { get; }
        int TokenColumnNumber { get; }
    }
}