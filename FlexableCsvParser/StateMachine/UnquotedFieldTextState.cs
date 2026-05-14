using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTextState : BaseState<UnquotedFieldTextState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, UnquotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.UnquotedFieldText;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = this;
        return true;
    }
}