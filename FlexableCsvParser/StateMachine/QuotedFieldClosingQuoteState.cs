using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteState : BaseState<QuotedFieldClosingQuoteState>
{
    public override ParserState Id => ParserState.QuotedFieldClosingQuote;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance)
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.Text, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Escape, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Quote, UnexpectedTokenState.Instance);
    }
}