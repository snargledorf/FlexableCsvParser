using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTextState : BaseState<UnquotedFieldTextState>
{
    public override ParserState Id => ParserState.UnquotedFieldText;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.FieldDelimiter, EndOfFieldState.Instance)
            .Add(CsvTokens.EndOfRecord, EndOfRecordState.Instance)
            .Add(CsvTokens.WhiteSpace, UnquotedFieldTrailingWhiteSpaceState.Instance);
    }
}