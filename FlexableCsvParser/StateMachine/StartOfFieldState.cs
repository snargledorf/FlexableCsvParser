using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class StartOfFieldState<T> : BaseState<T> where T : StartOfFieldState<T>, IState, new()
{
    public override ParserState Id => ParserState.StartOfField;
    
    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = null;
        return false;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Text, UnquotedFieldTextState.Instance)
            .Add(CsvTokens.Number, UnquotedFieldTextState.Instance)
            .Add(CsvTokens.Quote, QuotedFieldOpenQuoteState.Instance)
            .Add(CsvTokens.WhiteSpace, LeadingWhiteSpaceState.Instance)
            .Add(CsvTokens.FieldDelimiter, EndOfFieldState.Instance)
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.Escape, LeadingEscapeState.Instance);
    }
}

internal class StartOfFieldState : StartOfFieldState<StartOfFieldState>
{
}