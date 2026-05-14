using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class StartOfFieldState<T> : BaseState<T> where T : BaseState, IStateMapProvider, IDefaultStateProvider, new()
{
    public override ParserState Id => ParserState.StartOfField;
}

internal class StartOfFieldState : StartOfFieldState<StartOfFieldState>, IStateMapProvider, IDefaultStateProvider
{
    public static BaseState? DefaultState => null;

    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Text, UnquotedFieldTextState.Instance },
            { CsvTokens.Quote, QuotedFieldOpenQuoteState.Instance },
            { CsvTokens.WhiteSpace, LeadingWhiteSpaceState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.Escape, LeadingEscapeState.Instance }
        }.Build();
}