using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class LeadingWhiteSpaceState : BaseState<LeadingWhiteSpaceState>, IStateMapProvider, IDefaultStateProvider
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

    public static BaseState? DefaultState => EndOfFieldState.Instance;
    
    public override ParserState Id => ParserState.LeadingWhiteSpace;
}