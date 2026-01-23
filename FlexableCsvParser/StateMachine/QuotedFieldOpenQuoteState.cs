using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldOpenQuoteState : BaseState<QuotedFieldOpenQuoteState>
{
    public override ParserState Id => ParserState.QuotedFieldOpenQuote;

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
            nextState = QuotedFieldLeadingWhiteSpaceState.Instance;
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