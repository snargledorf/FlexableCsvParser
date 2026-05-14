using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteTrailingWhiteSpaceState : BaseState<QuotedFieldClosingQuoteTrailingWhiteSpaceState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace;

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