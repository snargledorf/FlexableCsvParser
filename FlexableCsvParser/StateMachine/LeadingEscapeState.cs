using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class LeadingEscapeState : BaseState<LeadingEscapeState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance },
            { CsvTokens.Escape, EscapeAfterLeadingEscapeState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance },
            { CsvTokens.Text, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.LeadingEscape;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}