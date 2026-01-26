using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTrailingWhiteSpaceState : BaseState<UnquotedFieldTrailingWhiteSpaceState>
{
    public override ParserState Id => ParserState.UnquotedFieldTrailingWhiteSpace;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Text)
        {
            nextState = UnquotedFieldTextState.Instance;
            return true;
        }
         
        if (token == CsvTokens.FieldDelimiter)
        {
            nextState = EndOfFieldState.Instance;
            return true;
        }
         
        if (token == CsvTokens.EndOfRecord)
        {
            nextState = EndOfRecordState.Instance;
            return true;
        }
        
        if (token == CsvTokens.WhiteSpace || token == CsvTokens.Escape || token == CsvTokens.Quote)
        {
            nextState = UnexpectedTokenState.Instance;
            return true;
        }
        
        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}