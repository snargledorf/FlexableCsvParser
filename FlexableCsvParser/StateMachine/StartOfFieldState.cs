using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class StartOfFieldState<T> : BaseState<T> where T : BaseState, new()
{
    public override ParserState Id => ParserState.StartOfField;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return StartOfFieldState.StateMap.TryGetState(token, out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = null;
        return false;
    }
}

internal class StartOfFieldState : StartOfFieldState<StartOfFieldState>
{
    internal static readonly StateMap StateMap =
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