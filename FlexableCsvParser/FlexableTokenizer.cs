using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using FastState;

namespace FlexableCsvParser
{
    public class FlexableTokenizer : Tokenizer
    {
        private readonly Memory<char> readBuffer = new char[4096];
        private int readBufferIndex;
        private int readBufferLength;

        private int columnIndex, lineIndex;

        private readonly StateMachine<int, char> stateMachine;

        public FlexableTokenizer(Delimiters delimiters)
            : base(delimiters)
        {
            stateMachine = TokenizerStateMachineFactory.CreateTokenizerStateMachine(delimiters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Token NextToken(TextReader reader)
        {
            var readBufferSpan = readBuffer.Span;

            int state = FlexableTokenizerTokenState.Start;

            StringBuilder valueBuilder = null;

            int startOfTokenColumnIndex = columnIndex;

            do
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
                do
                {
                    char c = workingBuffer[workingBufferIndex];

                    if (stateMachine.TryTransition(state, c, out int newState))
                    {
                        state = newState;
                        switch (state)
                        {
                            case FlexableTokenizerTokenState.EndOfText:
                                return CreateToken(TokenType.Text, startOfTokenColumnIndex, lineIndex, ref valueBuilder, workingBuffer[..workingBufferIndex]);

                            case FlexableTokenizerTokenState.EndOfWhiteSpace:
                                return CreateToken(TokenType.WhiteSpace, startOfTokenColumnIndex, lineIndex, ref valueBuilder, workingBuffer[..workingBufferIndex]);

                            case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                                return new Token(TokenType.FieldDelimiter, startOfTokenColumnIndex, lineIndex);

                            case FlexableTokenizerTokenState.EndOfEndOfRecord:
                                columnIndex = 0;
                                return new Token(TokenType.EndOfRecord, startOfTokenColumnIndex, lineIndex++);

                            case FlexableTokenizerTokenState.EndOfQuote:
                                return new Token(TokenType.Quote, startOfTokenColumnIndex, lineIndex);

                            case FlexableTokenizerTokenState.EndOfEscape:
                                return new Token(TokenType.Escape, startOfTokenColumnIndex, lineIndex);
                        }
                    }

                    columnIndex++;
                    readBufferIndex++;
                    workingBufferIndex++;
                } while (workingBufferIndex < workingBuffer.Length);

                valueBuilder ??= new StringBuilder(workingBuffer.Length + 80);
                valueBuilder.Append(workingBuffer);
            }
            while (true);

            if (state != FlexableTokenizerTokenState.Start && stateMachine.TryGetDefaultForState(state, out int defaultState))
                state = defaultState;

            switch (state)
            {
                case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                    return new Token(TokenType.FieldDelimiter, startOfTokenColumnIndex, lineIndex);
                case FlexableTokenizerTokenState.EndOfEndOfRecord:
                    columnIndex = 0;
                    return new Token(TokenType.EndOfRecord, startOfTokenColumnIndex, lineIndex++);
                case FlexableTokenizerTokenState.EndOfQuote:
                    return new Token(TokenType.Quote, startOfTokenColumnIndex, lineIndex);
                case FlexableTokenizerTokenState.EndOfEscape:
                    return new Token(TokenType.Escape, startOfTokenColumnIndex, lineIndex);
                case FlexableTokenizerTokenState.Start:
                    return new Token(TokenType.EndOfReader, startOfTokenColumnIndex, lineIndex);
                default:
                    return CreateToken(
                        state == FlexableTokenizerTokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text,
                        startOfTokenColumnIndex,
                        lineIndex,
                        ref valueBuilder,
                        ReadOnlySpan<char>.Empty);
            }
        }
    }

    struct FlexableTokenizerTokenState
    {
        public const int Start = 0;
        public const int FieldDelimiter = Start + 1;
        public const int EndOfFieldDelimiter = FieldDelimiter + 1;
        public const int EndOfRecord = EndOfFieldDelimiter + 1;
        public const int EndOfEndOfRecord = EndOfRecord + 1;
        public const int Quote = EndOfEndOfRecord + 1;
        public const int EndOfQuote = Quote + 1;
        public const int Escape = EndOfQuote + 1;
        public const int EndOfEscape = Escape + 1;
        public const int WhiteSpace = EndOfEscape + 1;
        public const int EndOfWhiteSpace = WhiteSpace + 1;
        public const int Text = EndOfWhiteSpace + 1;
        public const int EndOfText = Text + 1;
        public const int StartOfAdditionalStates = EndOfText + 1;
    }
}