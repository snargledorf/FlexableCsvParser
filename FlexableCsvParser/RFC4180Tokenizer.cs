using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlexableCsvParser
{
    public class RFC4180Tokenizer : Tokenizer
    {
        private char[] readBuffer = new char[4096];

        private int readBufferIndex;
        private int readBufferLength;

        private int tokenStartIndex;

        private int columnIndex, lineIndex;

        public RFC4180Tokenizer()
            : base(Delimiters.RFC4180)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Token NextToken(TextReader reader)
        {
            int state = TokenState.Start;

            int startOfTokenColumnIndex = columnIndex;
            tokenStartIndex = readBufferIndex;

            while (FillBuffer(reader))
            {
                do
                {
                    char c = readBuffer[readBufferIndex];

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
                                return CreateToken(TokenType.WhiteSpace, startOfTokenColumnIndex, lineIndex, new ReadOnlySpan<char>(readBuffer, tokenStartIndex, readBufferIndex - tokenStartIndex));
                            break;

                        case TokenState.Text:
                            if (c == ',' || c == '"' || char.IsWhiteSpace(c))
                                return CreateToken(TokenType.Text, startOfTokenColumnIndex, lineIndex, new ReadOnlySpan<char>(readBuffer, tokenStartIndex, readBufferIndex - tokenStartIndex));
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
                            return new Token(TokenType.FieldDelimiter, startOfTokenColumnIndex, lineIndex, FieldValue);

                        case TokenState.EndOfEndOfRecord:
                            columnIndex = 0;
                            return new Token(TokenType.EndOfRecord, startOfTokenColumnIndex, lineIndex++, EndOfRecordValue);

                        case TokenState.EndOfQuote:
                            return new Token(TokenType.Quote, startOfTokenColumnIndex, lineIndex, QuoteValue);

                        case TokenState.EndOfEscape:
                            return new Token(TokenType.Escape, startOfTokenColumnIndex, lineIndex, EscapeValue);
                    }

                    columnIndex++;
                    readBufferIndex++;
                }
                while (readBufferIndex < readBufferLength);
            }

            switch (state)
            {
                case TokenState.EndOfFieldDelimiter:
                    return new Token(TokenType.FieldDelimiter, startOfTokenColumnIndex, lineIndex, FieldValue);
                case TokenState.EndOfEndOfRecord:
                    columnIndex = 0;
                    return new Token(TokenType.EndOfRecord, startOfTokenColumnIndex, lineIndex++, EndOfRecordValue);
                case TokenState.EndOfQuote:
                case TokenState.StartOfEscape:
                    return new Token(TokenType.Quote, startOfTokenColumnIndex, lineIndex, QuoteValue);
                case TokenState.EndOfEscape:
                    return new Token(TokenType.Escape, startOfTokenColumnIndex, lineIndex, EscapeValue);
                case TokenState.Start:
                    return new Token(TokenType.EndOfReader, startOfTokenColumnIndex, lineIndex, null);
                default:
                    return CreateToken(
                        state == TokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text,
                        startOfTokenColumnIndex,
                        lineIndex,
                        new ReadOnlySpan<char>(readBuffer, tokenStartIndex, readBufferIndex - tokenStartIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FillBuffer(TextReader reader)
        {
            if (readBufferIndex < readBufferLength)
                return true;
                    
            if (tokenStartIndex != 0)
            {
                Array.Copy(readBuffer, tokenStartIndex, readBuffer, 0, readBufferLength - tokenStartIndex);
                readBufferIndex -= tokenStartIndex;
                readBufferLength -= tokenStartIndex;
                tokenStartIndex = 0;
            }
            else if (readBufferLength != 0)
            {
                Array.Resize(ref readBuffer, Math.Min(readBuffer.Length * 2, 0x7FFFFFC7));
            }

            readBufferIndex -= tokenStartIndex;
            readBufferLength -= tokenStartIndex;
            int charsToRead = readBuffer.Length - readBufferLength;
            readBufferLength += reader.Read(readBuffer, readBufferLength, charsToRead);

            return readBufferLength != readBufferIndex;
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