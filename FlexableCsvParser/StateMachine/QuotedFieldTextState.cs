using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTextState : BaseState<QuotedFieldTextState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.QuotedFieldText;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return StateMap.TryGetState(token, out nextState) || TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = Instance;
        return true;
    }
}