using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public class RFC4180Tokenizer : Tokenizer
    {
        private int columnIndex, lineIndex;

        public RFC4180Tokenizer()
            : base(Delimiters.RFC4180)
        {
        }

        protected override bool TryParseToken(in ReadOnlySpan<char> buffer, out TokenType type, out int startOfTokenColumnIndex, out int startOfTokenLineIndex, out int charCount)
        {
            TokenState state = TokenState.Start;

            startOfTokenColumnIndex = columnIndex;
            startOfTokenLineIndex = lineIndex;
            charCount = 0;

            foreach (char c in buffer)
            {
                switch (state)
                {
                    case TokenState.Start:
                        state = c switch
                        {
                            ',' => TokenState.EndOfFieldDelimiter,
                            '\r' => TokenState.StartOfEndOfRecord,
                            '\n' => TokenState.EndOfEndOfRecord,
                            '"' => TokenState.StartOfEscape,
                            _ => char.IsWhiteSpace(c) ? TokenState.WhiteSpace : TokenState.Text
                        };
                        break;

                    case TokenState.WhiteSpace:
                        if (!char.IsWhiteSpace(c) || c == '\r')
                        {
                            type = TokenType.WhiteSpace;
                            return true;
                        }
                        break;

                    case TokenState.Text:
                        if (c == ',' || c == '"' || char.IsWhiteSpace(c))
                        {
                            type = TokenType.Text;
                            return true;
                        }
                        break;

                    case TokenState.StartOfEndOfRecord:
                        if (c == '\n')
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
                        if (c == '"')
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

                if (c == '\n')
                {
                    lineIndex++;
                    columnIndex = 0;
                }
                else
                    columnIndex++;

                charCount++;
            }

            if (EndOfReader)
            {
                switch (state)
                {
                    case TokenState.EndOfFieldDelimiter:
                        type = TokenType.FieldDelimiter;
                        return true;
                    case TokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        return true;
                    case TokenState.EndOfQuote:
                    case TokenState.StartOfEscape:
                        type = TokenType.Quote;
                        return true;
                    case TokenState.EndOfEscape:
                        type = TokenType.Escape;
                        return true;
                    //case TokenState.Start:
                    //    token = CreateToken(TokenType.EndOfReader, startOfTokenColumnIndex, startOfTokenLineIndex);
                    //    return true;
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