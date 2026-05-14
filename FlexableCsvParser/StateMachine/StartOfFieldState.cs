using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class StartOfFieldState<T> : BaseState<T> where T : BaseState, IStateMapProvider, new()
{
    public override ParserState Id => ParserState.StartOfField;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = null;
        return false;
    }
}

internal class StartOfFieldState : StartOfFieldState<StartOfFieldState>, IStateMapProvider
{
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