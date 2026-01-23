using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTrailingWhiteSpaceState : BaseState<QuotedFieldTrailingWhiteSpaceState>
{
    public override ParserState Id => ParserState.QuotedFieldTrailingWhiteSpace;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Quote)
        {
            nextState = QuotedFieldClosingQuoteState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Escape)
        {
            nextState = QuotedFieldEscapeState.Instance;
            return true;
        }
        
        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}