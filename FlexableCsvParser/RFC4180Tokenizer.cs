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

        private int columnIndex, lineIndex;

        private TokenState state = TokenState.Start;
        private int charCount;

        private int startOfTokenColumnIndex;
        private int startOfTokenLineIndex;

        public RFC4180Tokenizer(TextReader reader)
            : base(reader, Delimiters.RFC4180)
        {
        }

        protected override bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int startOfTokenColumnIndex, out int startOfTokenLineIndex, out int charCount)
        {
            charCount = this.charCount;

            if (charCount == 0)
            {
                this.startOfTokenColumnIndex = startOfTokenColumnIndex = columnIndex;
                this.startOfTokenLineIndex = startOfTokenLineIndex = lineIndex;
            }
            else
            {
                startOfTokenColumnIndex = this.startOfTokenColumnIndex;
                startOfTokenLineIndex = this.startOfTokenLineIndex;
            }

            while (!buffer.IsEmpty)
            {
                char c = buffer[0];
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
                            ResetState();
                            return true;
                        }
                        break;

                    case TokenState.Text:
                        if (c == Comma || c == DoubleQuote || char.IsWhiteSpace(c))
                        {
                            type = TokenType.Text;
                            ResetState();
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
                        ResetState();
                        return true;

                    case TokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        ResetState();
                        return true;

                    case TokenState.EndOfQuote:
                        type = TokenType.Quote;
                        ResetState();
                        return true;

                    case TokenState.EndOfEscape:
                        type = TokenType.Escape;
                        ResetState();
                        return true;
                }

                if (c == LineFeedCharacter)
                {
                    lineIndex++;
                    columnIndex = 0;
                }
                else
                {
                    columnIndex++;
                }

                charCount++;
                buffer = buffer[1..];
            }

            if (endOfReader)
            {
                switch (state)
                {
                    case TokenState.EndOfFieldDelimiter:
                        type = TokenType.FieldDelimiter;
                        ResetState();
                        return true;
                    case TokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        ResetState();
                        return true;
                    case TokenState.EndOfQuote:
                    case TokenState.StartOfEscape:
                        type = TokenType.Quote;
                        ResetState();
                        return true;
                    case TokenState.EndOfEscape:
                        type = TokenType.Escape;
                        ResetState();
                        return true;
                    case TokenState.WhiteSpace:
                        type = TokenType.WhiteSpace;
                        ResetState();
                        return true;
                    case TokenState.Text:
                        type = TokenType.Text;
                        ResetState();
                        return true;
                }
            }

            this.charCount = charCount;
            type = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetState()
        {
            state = TokenState.Start;
            charCount = 0;
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