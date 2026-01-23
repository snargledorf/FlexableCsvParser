using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteTrailingWhiteSpaceState : BaseState<QuotedFieldClosingQuoteTrailingWhiteSpaceState>
{
    public override ParserState Id => ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.Text, UnexpectedTokenState.Instance)
            .Add(CsvTokens.WhiteSpace, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Escape, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Quote, UnexpectedTokenState.Instance);
    }
}