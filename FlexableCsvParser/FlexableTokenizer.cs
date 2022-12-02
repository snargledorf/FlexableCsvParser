using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using FastState;

namespace FlexableCsvParser
{
    public class FlexableTokenizer : Tokenizer
    {
        private int columnIndex, lineIndex;

        private readonly StateMachine<FlexableTokenizerTokenState, char> stateMachine;

        public FlexableTokenizer(Delimiters delimiters)
            : base(delimiters)
        {
            stateMachine = TokenizerStateMachineFactory.CreateTokenizerStateMachine(delimiters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async ValueTask<Token> NextTokenAsync(TextReader reader)
        {
            var state = FlexableTokenizerTokenState.Start;

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

                if (stateMachine.TryTransition(state, CurrentChar, out FlexableTokenizerTokenState newState))
                {
                    state = newState;
                    switch (state)
                    {
                        case FlexableTokenizerTokenState.EndOfText:
                            return CreateToken(TokenType.Text, startOfTokenColumnIndex, startOfTokenLineIndex);

                        case FlexableTokenizerTokenState.EndOfWhiteSpace:
                            return CreateToken(TokenType.WhiteSpace, startOfTokenColumnIndex, startOfTokenLineIndex);

                        case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                            return CreateToken(TokenType.FieldDelimiter, startOfTokenColumnIndex, startOfTokenLineIndex);

                        case FlexableTokenizerTokenState.EndOfEndOfRecord:
                            columnIndex = 0;
                            lineIndex++;
                            return CreateToken(TokenType.EndOfRecord, startOfTokenColumnIndex, startOfTokenLineIndex);

                        case FlexableTokenizerTokenState.EndOfQuote:
                            return CreateToken(TokenType.Quote, startOfTokenColumnIndex, startOfTokenLineIndex);

                        case FlexableTokenizerTokenState.EndOfEscape:
                            return CreateToken(TokenType.Escape, startOfTokenColumnIndex, startOfTokenLineIndex);
                    }
                }

                columnIndex++;
                MoveToNextChar();
            }


            if (state != FlexableTokenizerTokenState.Start && stateMachine.TryGetDefaultForState(state, out FlexableTokenizerTokenState defaultState))
                state = defaultState;

            switch (state)
            {
                case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                    return CreateToken(TokenType.FieldDelimiter, startOfTokenColumnIndex, lineIndex);
                case FlexableTokenizerTokenState.EndOfEndOfRecord:
                    columnIndex = 0;
                    lineIndex++;
                    return CreateToken(TokenType.EndOfRecord, startOfTokenColumnIndex, startOfTokenLineIndex);
                case FlexableTokenizerTokenState.EndOfQuote:
                    return CreateToken(TokenType.Quote, startOfTokenColumnIndex, startOfTokenLineIndex);
                case FlexableTokenizerTokenState.EndOfEscape:
                    return CreateToken(TokenType.Escape, startOfTokenColumnIndex, startOfTokenLineIndex);
                case FlexableTokenizerTokenState.Start:
                    return CreateToken(TokenType.EndOfReader, startOfTokenColumnIndex, startOfTokenLineIndex);
                default:
                    return CreateToken(
                        state == FlexableTokenizerTokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text,
                        startOfTokenColumnIndex,
                        lineIndex);
            }
        }
    }

    /*struct FlexableTokenizerTokenState
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
    }*/

    enum FlexableTokenizerTokenState
    {
        Start = 0,
        FieldDelimiter,
        EndOfFieldDelimiter,
        EndOfRecord,
        EndOfEndOfRecord,
        Quote,
        EndOfQuote,
        Escape,
        EndOfEscape,
        WhiteSpace,
        EndOfWhiteSpace,
        Text,
        EndOfText,
        StartOfAdditionalStates,
    }
}