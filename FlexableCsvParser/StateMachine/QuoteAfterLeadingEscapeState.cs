using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuoteAfterLeadingEscapeState : BaseState<QuoteAfterLeadingEscapeState>
{
    public override ParserState Id => ParserState.QuoteAfterLeadingEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.WhiteSpace)
        {
            nextState = QuotedFieldTrailingWhiteSpaceState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Quote || token == CsvTokens.Escape)
        {
            nextState = UnexpectedTokenState.Instance;
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