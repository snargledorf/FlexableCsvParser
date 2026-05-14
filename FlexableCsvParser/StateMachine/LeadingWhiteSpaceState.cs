using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class LeadingWhiteSpaceState : BaseState<LeadingWhiteSpaceState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Text, UnquotedFieldTextState.Instance },
            { CsvTokens.Quote, QuotedFieldOpenQuoteState.Instance },
            { CsvTokens.WhiteSpace, UnexpectedTokenState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance },
            { CsvTokens.Escape, LeadingEscapeState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.LeadingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}