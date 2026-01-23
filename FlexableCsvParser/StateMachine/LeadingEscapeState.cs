using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class LeadingEscapeState : BaseState<LeadingEscapeState>
{
    public override ParserState Id => ParserState.LeadingEscape;
    
    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance)
            .Add(CsvTokens.Escape, EscapeAfterLeadingEscapeState.Instance)
            .Add(CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance)
            .Add(CsvTokens.Text, UnexpectedTokenState.Instance);
    }
}