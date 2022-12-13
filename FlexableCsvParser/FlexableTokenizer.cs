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
        private readonly StateMachine<FlexableTokenizerTokenState, char> stateMachine;

        public FlexableTokenizer(Delimiters delimiters)
            : base(delimiters)
        {
            stateMachine = TokenizerStateMachineFactory.CreateTokenizerStateMachine(delimiters);
        }

        public override bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int tokenLength)
        {
            tokenLength = 0;
            var state = FlexableTokenizerTokenState.Start;

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

                tokenLength++;
            }

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
            }

            if (endOfReader)
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