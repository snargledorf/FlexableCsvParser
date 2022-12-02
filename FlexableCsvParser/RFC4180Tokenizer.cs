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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask<Token> NextTokenAsync(TextReader reader)
        {
            TokenState state = TokenState.Start;

            int startOfTokenColumnIndex = columnIndex;
            int startOfTokenLineIndex = lineIndex;

            while (true)
            {
                if (EndOfBuffer)
                {
                    await FillBufferAsync(reader).ConfigureAwait(false);
                    if (EndOfBuffer)
                        break;
                }

                char c = CurrentChar;
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
                            return CreateToken(TokenType.WhiteSpace, startOfTokenColumnIndex, startOfTokenLineIndex);
                        break;

                    case TokenState.Text:
                        if (c == ',' || c == '"' || char.IsWhiteSpace(c))
                            return CreateToken(TokenType.Text, startOfTokenColumnIndex, startOfTokenLineIndex);
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
                        return CreateToken(TokenType.FieldDelimiter, startOfTokenColumnIndex, startOfTokenLineIndex);

                    case TokenState.EndOfEndOfRecord:
                        columnIndex = 0;
                        lineIndex++;
                        return CreateToken(TokenType.EndOfRecord, startOfTokenColumnIndex, startOfTokenLineIndex);

                    case TokenState.EndOfQuote:
                        return CreateToken(TokenType.Quote, startOfTokenColumnIndex, startOfTokenLineIndex);

                    case TokenState.EndOfEscape:
                        return CreateToken(TokenType.Escape, startOfTokenColumnIndex, startOfTokenLineIndex);
                }

                columnIndex++;
                MoveToNextChar();
            }

            switch (state)
            {
                case TokenState.EndOfFieldDelimiter:
                    return CreateToken(TokenType.FieldDelimiter, startOfTokenColumnIndex, startOfTokenLineIndex);
                case TokenState.EndOfEndOfRecord:
                    columnIndex = 0;
                    lineIndex++;
                    return CreateToken(TokenType.EndOfRecord, startOfTokenColumnIndex, startOfTokenLineIndex);
                case TokenState.EndOfQuote:
                case TokenState.StartOfEscape:
                    return CreateToken(TokenType.Quote, startOfTokenColumnIndex, startOfTokenLineIndex);
                case TokenState.EndOfEscape:
                    return CreateToken(TokenType.Escape, startOfTokenColumnIndex, startOfTokenLineIndex);
                case TokenState.Start:
                    return CreateToken(TokenType.EndOfReader, startOfTokenColumnIndex, startOfTokenLineIndex);
                default:
                    return CreateToken(
                        state == TokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text,
                        startOfTokenColumnIndex,
                        lineIndex);
            }
        }
    }

    /*struct TokenState
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
    }*/

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