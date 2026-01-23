using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class EscapeAfterLeadingEscapeState : BaseState<EscapeAfterLeadingEscapeState>
{
    public override ParserState Id => ParserState.EscapeAfterLeadingEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Escape)
        {
            nextState = Instance;
            return true;
        }
        
        if (token == CsvTokens.Quote)
        {
            nextState = QuoteAfterLeadingEscapeState.Instance;
            return true;
        }
        
        if (token == CsvTokens.FieldDelimiter)
        {
            nextState = EndOfFieldState.Instance;
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