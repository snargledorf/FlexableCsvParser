using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTrailingWhiteSpaceState : BaseState<UnquotedFieldTrailingWhiteSpaceState>, IStateMapProvider
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

    public override ParserState Id => ParserState.UnquotedFieldTrailingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}