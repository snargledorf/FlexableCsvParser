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

        protected override bool TryParseToken(in ReadOnlySpan<char> buffer, out TokenType type, out int startOfTokenColumnIndex, out int startOfTokenLineIndex, out int charCount)
        {
            var state = FlexableTokenizerTokenState.Start;

            startOfTokenColumnIndex = columnIndex;
            startOfTokenLineIndex = lineIndex;
            charCount = 0;

            foreach (char c in buffer)
            {
                if (stateMachine.TryTransition(state, c, out FlexableTokenizerTokenState newState))
                {
                    state = newState;
                    switch (state)
                    {
                        case FlexableTokenizerTokenState.EndOfText:
                            type = TokenType.Text;
                            return true;

                        case FlexableTokenizerTokenState.EndOfWhiteSpace:
                            type = TokenType.WhiteSpace;
                            return true;

                        case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                            type = TokenType.FieldDelimiter;
                            return true;

                        case FlexableTokenizerTokenState.EndOfEndOfRecord:
                            type = TokenType.EndOfRecord;
                            return true;

                        case FlexableTokenizerTokenState.EndOfQuote:
                            type = TokenType.Quote;
                            return true;

                        case FlexableTokenizerTokenState.EndOfEscape:
                            type = TokenType.Escape;
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

            if (EndOfReader)
            {
                if (state != FlexableTokenizerTokenState.Start && stateMachine.TryGetDefaultForState(state, out FlexableTokenizerTokenState defaultState))
                    state = defaultState;

                switch (state)
                {
                    case FlexableTokenizerTokenState.EndOfFieldDelimiter:
                        type = TokenType.FieldDelimiter;
                        return true;
                    case FlexableTokenizerTokenState.EndOfEndOfRecord:
                        type = TokenType.EndOfRecord;
                        return true;
                    case FlexableTokenizerTokenState.EndOfQuote:
                        type = TokenType.Quote;
                        return true;
                    case FlexableTokenizerTokenState.EndOfEscape:
                        type = TokenType.Escape;
                        return true;
                    //case FlexableTokenizerTokenState.Start:
                    //    token = CreateToken(TokenType.EndOfReader, startOfTokenColumnIndex, startOfTokenLineIndex);
                    //    return true;
                    case FlexableTokenizerTokenState.WhiteSpace:
                        type = TokenType.WhiteSpace;
                        return true;
                    case FlexableTokenizerTokenState.Text:
                        type = TokenType.Text;
                        return true;
                }
            }

            type = default;
            return false;
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