using System.Runtime.CompilerServices;
using System.Text;

using CsvSpanParser.StateMachine;

namespace CsvSpanParser
{
    public class FlexableTokenizer : Tokenizer
    {
        private readonly Memory<char> readBuffer = new char[4096];
        private int readBufferIndex = 0;
        private int readBufferLength = 0;

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

            StringBuilder? valueBuilder = null;

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
                                return Token.FieldDelimiter;

                            case FlexableTokenizerTokenState.EndOfEndOfRecord:
                                return Token.EndOfRecord;

                            case FlexableTokenizerTokenState.EndOfQuote:
                                return Token.Quote;

                            case FlexableTokenizerTokenState.EndOfEscape:
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

            if (state != FlexableTokenizerTokenState.Start && stateMachine.TryGetDefaultForState(state, out int newState))
                state = newState;

            return state switch
            {
                FlexableTokenizerTokenState.EndOfFieldDelimiter => Token.FieldDelimiter,
                FlexableTokenizerTokenState.EndOfEndOfRecord => Token.EndOfRecord,
                FlexableTokenizerTokenState.EndOfQuote => Token.Quote,
                FlexableTokenizerTokenState.EndOfEscape => Token.Escape,
                FlexableTokenizerTokenState.Start => Token.EndOfReader,
                _ => CreateToken(state == FlexableTokenizerTokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text, ref valueBuilder, ReadOnlySpan<char>.Empty)
            };
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