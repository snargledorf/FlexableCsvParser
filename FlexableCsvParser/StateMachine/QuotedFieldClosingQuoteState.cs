using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteState : BaseState<QuotedFieldClosingQuoteState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuotedFieldClosingQuote;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}