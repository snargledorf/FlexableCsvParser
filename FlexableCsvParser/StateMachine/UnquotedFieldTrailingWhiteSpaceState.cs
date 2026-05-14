using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTrailingWhiteSpaceState : BaseState<UnquotedFieldTrailingWhiteSpaceState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Text, UnquotedFieldTextState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.WhiteSpace, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance }
        }.Build();

    public static BaseState? DefaultState => EndOfFieldState.Instance;

    public override ParserState Id => ParserState.UnquotedFieldTrailingWhiteSpace;
}