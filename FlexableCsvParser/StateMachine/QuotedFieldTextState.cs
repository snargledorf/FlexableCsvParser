using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTextState : BaseState<QuotedFieldTextState>
{
    public override ParserState Id => ParserState.QuotedFieldText;

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

        if (token == CsvTokens.WhiteSpace)
        {
            nextState = QuotedFieldTrailingWhiteSpaceState.Instance;
            return true;
        }
        
        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = Instance;
        return true;
    }
}