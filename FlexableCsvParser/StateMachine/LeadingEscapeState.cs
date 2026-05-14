using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class LeadingEscapeState : BaseState<LeadingEscapeState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance },
            { CsvTokens.Escape, EscapeAfterLeadingEscapeState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.LeadingEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return StateMap.TryGetState(token, out nextState) || TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}