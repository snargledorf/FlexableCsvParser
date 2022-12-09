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

        private FlexableTokenizerTokenState state = FlexableTokenizerTokenState.Start;
        private int charCount;

        private int startOfTokenColumnIndex;
        private int startOfTokenLineIndex;

        private readonly StateMachine<FlexableTokenizerTokenState, char> stateMachine;

        public FlexableTokenizer(TextReader reader, Delimiters delimiters)
            : base(reader, delimiters)
        {
            stateMachine = TokenizerStateMachineFactory.CreateTokenizerStateMachine(delimiters);
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

            foreach (char c in buffer)
            {
                if (stateMachine.TryTransition(state, c, out FlexableTokenizerTokenState newState))
                {
                    state = newState;
                    switch (state)
                    {
                        case FlexableTokenizerTokenState.EndOfText:
                            type = TokenType.Text;
                            ResetState();
                            return true;

                        case FlexableTokenizerTokenState.EndOfWhiteSpace:
                            type = TokenType.WhiteSpace;
                            ResetState();
                            return true;

                        case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                            type = TokenType.FieldDelimiter;
                            ResetState();
                            return true;

                        case FlexableTokenizerTokenState.EndOfEndOfRecord:
                            type = TokenType.EndOfRecord;
                            ResetState();
                            return true;

                        case FlexableTokenizerTokenState.EndOfQuote:
                            type = TokenType.Quote;
                            ResetState();
                            return true;

                        case FlexableTokenizerTokenState.EndOfEscape:
                            type = TokenType.Escape;
                            ResetState();
                            return true;
                    }
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

            if (endOfReader)
            {
                if (state != FlexableTokenizerTokenState.Start && stateMachine.TryGetDefaultForState(state, out FlexableTokenizerTokenState defaultState))
                    state = defaultState;

                switch (state)
                {
                    case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                        type = TokenType.FieldDelimiter;
                        ResetState();
                        return true;
                    case FlexableTokenizerTokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        ResetState();
                        return true;
                    case FlexableTokenizerTokenState.EndOfQuote:
                        type = TokenType.Quote;
                        ResetState();
                        return true;
                    case FlexableTokenizerTokenState.EndOfEscape:
                        type = TokenType.Escape;
                        ResetState();
                        return true;
                    case FlexableTokenizerTokenState.WhiteSpace:
                        type = TokenType.WhiteSpace;
                        ResetState();
                        return true;
                    case FlexableTokenizerTokenState.Text:
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
            state = FlexableTokenizerTokenState.Start;
            charCount = 0;
        }
    }

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