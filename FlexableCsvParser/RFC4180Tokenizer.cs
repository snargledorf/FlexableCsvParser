using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlexableCsvParser
{
    public class RFC4180Tokenizer : Tokenizer
    {
        private readonly Memory<char> readBuffer = new char[4096];
        private int readBufferIndex;
        private int readBufferLength;

        public RFC4180Tokenizer()
            : base(Delimiters.RFC4180)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Token NextToken(TextReader reader)
        {
            var readBufferSpan = readBuffer.Span;

            int state = TokenState.Start;

            StringBuilder valueBuilder = null;

            while (true)
            {
                if (readBufferIndex >= readBufferLength)
                {
                    readBufferLength = reader.Read(readBufferSpan);
                    readBufferIndex = 0;
                }

                if (readBufferLength == 0)
                    break;

                ReadOnlySpan<char> workingBuffer = readBufferSpan[readBufferIndex..readBufferLength];
                int workingBufferIndex = 0;

                while (workingBufferIndex < workingBuffer.Length)
                {
                    char c = workingBuffer[workingBufferIndex];

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
                                return CreateToken(TokenType.WhiteSpace, ref valueBuilder, workingBuffer[..workingBufferIndex]);
                            break;

                        case TokenState.Text:
                            if (char.IsWhiteSpace(c) || c == ',' || c == '"')
                                return CreateToken(TokenType.Text, ref valueBuilder, workingBuffer[..workingBufferIndex]);
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
                                state = TokenState.EndOfQuote;
                                goto case TokenState.EndOfQuote;
                            }
                            break;

                        case TokenState.EndOfFieldDelimiter:
                            return Token.FieldDelimiter;

                        case TokenState.EndOfEndOfRecord:
                            return Token.EndOfRecord;

                        case TokenState.EndOfQuote:
                            return Token.Quote;

                        case TokenState.EndOfEscape:
                            return Token.Escape;
                    }

                    readBufferIndex++;
                    workingBufferIndex++;
                }

                valueBuilder ??= new StringBuilder(workingBuffer.Length + 80);
                valueBuilder.Append(workingBuffer);
            }

            switch (state)
            {
                case TokenState.EndOfFieldDelimiter:
                    return Token.FieldDelimiter;
                case TokenState.EndOfEndOfRecord:
                    return Token.EndOfRecord;
                case TokenState.EndOfQuote:
                case TokenState.StartOfEscape:
                    return Token.Quote;
                case TokenState.EndOfEscape:
                    return Token.Escape;
                case TokenState.Start:
                    return Token.EndOfReader;
                default:
                    return CreateToken(state == TokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text, ref valueBuilder, ReadOnlySpan<char>.Empty);
            }
        }
    }

    struct TokenState
    {
        public const int Start = 0;
        public const int EndOfFieldDelimiter = Start + 1;
        public const int StartOfEndOfRecord = EndOfFieldDelimiter + 1;
        public const int EndOfEndOfRecord = StartOfEndOfRecord + 1;
        public const int EndOfQuote = EndOfEndOfRecord + 1;
        public const int StartOfEscape = EndOfQuote + 1;
        public const int EndOfEscape = StartOfEscape + 1;
        public const int WhiteSpace = EndOfEscape + 1;
        public const int EndOfWhiteSpace = WhiteSpace + 1;
        public const int Text = EndOfWhiteSpace + 1;
        public const int EndOfText = Text + 1;

    }
}