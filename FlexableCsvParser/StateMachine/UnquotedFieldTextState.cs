using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTextState : BaseState<UnquotedFieldTextState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, UnquotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance }
        }.Build();

    public static BaseState? DefaultState => Instance;
    
    public override ParserState Id => ParserState.UnquotedFieldText;
}