using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class LeadingEscapeState : BaseState<LeadingEscapeState>
{
    public override ParserState Id => ParserState.LeadingEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Quote)
        {
            nextState = QuoteAfterLeadingEscapeState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Escape)
        {
            nextState = EscapeAfterLeadingEscapeState.Instance;
            return true;
        }

        if (token == CsvTokens.WhiteSpace)
        {
            nextState = QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Text)
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