using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTrailingWhiteSpaceState : BaseState<UnquotedFieldTrailingWhiteSpaceState>
{
    public override ParserState Id => ParserState.UnquotedFieldTrailingWhiteSpace;

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
            .Add(CsvTokens.FieldDelimiter, EndOfFieldState.Instance)
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.WhiteSpace, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Escape, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Quote, UnexpectedTokenState.Instance);
    }
}