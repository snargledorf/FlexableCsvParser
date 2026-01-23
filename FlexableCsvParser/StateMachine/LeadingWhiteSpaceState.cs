using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class LeadingWhiteSpaceState : BaseState<LeadingWhiteSpaceState>
{
    public override ParserState Id => ParserState.LeadingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Text, UnquotedFieldTextState.Instance)
            .Add(CsvTokens.Number, UnquotedFieldTextState.Instance)
            .Add(CsvTokens.Quote, QuotedFieldOpenQuoteState.Instance)
            .Add(CsvTokens.FieldDelimiter, EndOfFieldState.Instance)
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.Escape, LeadingEscapeState.Instance)
            .Add(CsvTokens.WhiteSpace, UnexpectedTokenState.Instance);
    }
}