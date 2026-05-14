using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class EscapeAfterLeadingEscapeState : BaseState<EscapeAfterLeadingEscapeState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Escape, EscapeAfterLeadingEscapeState.Instance },
            { CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance }
        }.Build();

    public static BaseState? DefaultState => EndOfFieldState.Instance;

    public override ParserState Id => ParserState.EscapeAfterLeadingEscape;
}