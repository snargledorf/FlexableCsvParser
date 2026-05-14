using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class EscapeAfterLeadingEscapeState : BaseState<EscapeAfterLeadingEscapeState>, IStateMapProvider
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

    public override ParserState Id => ParserState.EscapeAfterLeadingEscape;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}