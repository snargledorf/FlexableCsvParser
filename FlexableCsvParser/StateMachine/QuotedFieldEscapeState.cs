using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldEscapeState : BaseState<QuotedFieldEscapeState>
{
    public override ParserState Id => ParserState.QuotedFieldEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Quote)
        {
            nextState = QuotedFieldClosingQuoteState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Escape)
        {
            nextState = Instance;
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