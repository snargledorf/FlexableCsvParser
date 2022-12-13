using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public class RFC4180Tokenizer : Tokenizer
    {
        private const char Comma = ',';
        private const char CariageReturn = '\r';
        private const char LineFeedCharacter = '\n';
        private const char DoubleQuote = '"';

        public RFC4180Tokenizer()
            : base(Delimiters.RFC4180)
        {
        }

        public override bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int tokenLength)
        {
            var state = TokenState.Start;
            tokenLength = 0;

            foreach (var c in buffer)
            {
                switch (state)
                {
                    case TokenState.Start:
                        state = c switch
                        {
                            Comma => TokenState.EndOfFieldDelimiter,
                            CariageReturn => TokenState.StartOfEndOfRecord,
                            LineFeedCharacter => TokenState.EndOfEndOfRecord,
                            DoubleQuote => TokenState.StartOfEscape,
                            _ => char.IsWhiteSpace(c) ? TokenState.WhiteSpace : TokenState.Text
                        };
                        break;

                    case TokenState.WhiteSpace:
                        if (!char.IsWhiteSpace(c) || c == CariageReturn)
                        {
                            type = TokenType.WhiteSpace;
                            return true;
                        }
                        break;

                    case TokenState.Text:
                        if (c == Comma || c == DoubleQuote || char.IsWhiteSpace(c))
                        {
                            type = TokenType.Text;
                            return true;
                        }
                        break;

                    case TokenState.StartOfEndOfRecord:
                        if (c == LineFeedCharacter)
                        {
                            state = TokenState.EndOfEndOfRecord;
                        }
                        else
                        {
                            state = TokenState.WhiteSpace;
                            goto case TokenState.WhiteSpace;
                        }
                        break;

                    case TokenState.StartOfEscape:
                        if (c == DoubleQuote)
                        {
                            state = TokenState.EndOfEscape;
                        }
                        else
                        {
                            goto case TokenState.EndOfQuote;
                        }
                        break;

                    case TokenState.EndOfFieldDelimiter:
                        type = TokenType.FieldDelimiter;
                        return true;

                    case TokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        return true;

                    case TokenState.EndOfQuote:
                        type = TokenType.Quote;
                        return true;

                    case TokenState.EndOfEscape:
                        type = TokenType.Escape;
                        return true;
                }

                tokenLength++;
            }

            switch (state)
            {
                case TokenState.EndOfFieldDelimiter:
                    type = TokenType.FieldDelimiter;
                    return true;
                case TokenState.EndOfEndOfRecord:
                    type = TokenType.EndOfRecord;
                    return true;
                case TokenState.EndOfQuote:
                    type = TokenType.Quote;
                    return true;
                case TokenState.EndOfEscape:
                    type = TokenType.Escape;
                    return true;
            }

            if (endOfReader)
            {
                switch (state)
                {
                    case TokenState.StartOfEscape:
                        type = TokenType.Quote;
                        return true;
                    case TokenState.WhiteSpace:
                        type = TokenType.WhiteSpace;
                        return true;
                    case TokenState.Text:
                        type = TokenType.Text;
                        return true;
                }
            }

            type = default;
            return false;
        }
    }

    enum TokenState
    {
        Start = 0,
        EndOfFieldDelimiter,
        StartOfEndOfRecord,
        EndOfEndOfRecord,
        EndOfQuote,
        StartOfEscape,
        EndOfEscape,
        WhiteSpace,
        EndOfWhiteSpace,
        Text,
        EndOfText,
    }
}