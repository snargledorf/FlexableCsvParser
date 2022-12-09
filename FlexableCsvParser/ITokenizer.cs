using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public interface ITokenizer
    {
        IEnumerable<ITokenizer> EnumerateTokens();

        bool Read();

        TokenType TokenType { get; }
        ReadOnlySpan<char> TokenValue { get; }
        int TokenLineNumber { get; }
        int TokenColumnNumber { get; }
    }
}