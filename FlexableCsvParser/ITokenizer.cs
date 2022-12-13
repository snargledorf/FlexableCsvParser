using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public interface ITokenizer
    {
        bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int tokenLength);
    }
}