using System.Runtime.CompilerServices;
using System.Text;

using CsvSpanParser.StateMachine;

namespace CsvSpanParser
{
    public class FlexableTokenizer : Tokenizer
    {
        private readonly StateMachine<int, char> stateMachine;

        private readonly Memory<char> readBuffer = new char[4096];
        int readBufferIndex;
        int readBufferLength;

        public FlexableTokenizer(TextReader reader, TokenizerConfig config)
            : base(reader, config)
        {
            stateMachine = TokenizerStateMachineFactory.CreateTokenizerStateMachine(config);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Token ReadToken()
        {
            int state = FlexableTokenizerTokenState.Start;
            int workingBufferIndex = 0;

            ReadOnlySpan<char> workingBuffer = ReadOnlySpan<char>.Empty;
            StringBuilder? valueBuilder = null;

            while (CheckReadBuffer())
            {
                workingBuffer = readBuffer.Span[readBufferIndex..readBufferLength];
                workingBufferIndex = 0;
                do
                {
                    char c = workingBuffer[workingBufferIndex];

                    if (stateMachine.TryTransition(state, c, out state))
                    {
                        switch (state)
                        {
                            case FlexableTokenizerTokenState.EndOfText:
                                return CreateToken(TokenType.Text, ref valueBuilder, workingBuffer[..workingBufferIndex]);

                            case FlexableTokenizerTokenState.EndOfWhiteSpace:
                                return CreateToken(TokenType.WhiteSpace, ref valueBuilder, workingBuffer[..workingBufferIndex]);

                            case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                                readBufferIndex++;
                                return Token.FieldDelimiter;

                            case FlexableTokenizerTokenState.EndOfEndOfRecord:
                                readBufferIndex++;
                                return Token.EndOfRecord;

                            case FlexableTokenizerTokenState.EndOfQuote:
                                readBufferIndex++;
                                return Token.Quote;

                            case FlexableTokenizerTokenState.EndOfEscape:
                                readBufferIndex++;
                                return Token.Escape;
                        }
                    }

                    readBufferIndex++;
                    workingBufferIndex++;
                } while (workingBufferIndex < workingBuffer.Length);

                if (valueBuilder is null)
                    valueBuilder = new StringBuilder(workingBuffer.Length + 10).Append(workingBuffer);
                else
                    valueBuilder.Append(workingBuffer);
            }

            return state switch
            {
                FlexableTokenizerTokenState.EndOfFieldDelimiter => Token.FieldDelimiter,
                FlexableTokenizerTokenState.EndOfEndOfRecord => Token.EndOfRecord,
                FlexableTokenizerTokenState.EndOfQuote => Token.Quote,
                FlexableTokenizerTokenState.EndOfEscape => Token.Escape,
                FlexableTokenizerTokenState.Start => Token.EndOfReader,
                _ => CreateToken(state == FlexableTokenizerTokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text, ref valueBuilder, ReadOnlySpan<char>.Empty)
            };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Token CreateToken(in TokenType type, ref StringBuilder? valueBuilder, in ReadOnlySpan<char> buffer)
            {
                return new Token(type, valueBuilder?.Append(buffer).ToString() ?? new string(buffer));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckReadBuffer()
        {
            if (readBufferIndex >= readBufferLength)
            {
                readBufferLength = Reader.Read(readBuffer.Span);
                readBufferIndex = 0;
            }

            return readBufferLength != 0;
        }
    }

    struct FlexableTokenizerTokenState
    {
        public const int Start = 0;
        public const int EndOfFieldDelimiter = Start + 1;
        public const int EndOfEndOfRecord = EndOfFieldDelimiter + 1;
        public const int EndOfQuote = EndOfEndOfRecord + 1;
        public const int EndOfEscape = EndOfQuote + 1;
        public const int WhiteSpace = EndOfEscape + 1;
        public const int EndOfWhiteSpace = WhiteSpace + 1;
        public const int Text = EndOfWhiteSpace + 1;
        public const int EndOfText = Text + 1;
        public const int StartOfAdditionalStates = EndOfText + 1;
    }
}