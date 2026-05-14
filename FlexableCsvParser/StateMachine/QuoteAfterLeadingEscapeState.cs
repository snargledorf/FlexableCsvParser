using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuoteAfterLeadingEscapeState : BaseState<QuoteAfterLeadingEscapeState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuoteAfterLeadingEscape;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return StateMap.TryGetState(token, out nextState) || TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}