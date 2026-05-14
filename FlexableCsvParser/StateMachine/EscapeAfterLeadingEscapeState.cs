using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class EscapeAfterLeadingEscapeState : BaseState<EscapeAfterLeadingEscapeState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.Escape, Instance },
            { CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.EscapeAfterLeadingEscape;

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