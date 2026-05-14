using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldOpenQuoteState : BaseState<QuotedFieldOpenQuoteState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldLeadingWhiteSpaceState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.QuotedFieldOpenQuote;

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