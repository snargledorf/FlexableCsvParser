using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteTrailingWhiteSpaceState : BaseState<QuotedFieldClosingQuoteTrailingWhiteSpaceState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}