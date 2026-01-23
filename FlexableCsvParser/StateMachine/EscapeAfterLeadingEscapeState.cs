using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class EscapeAfterLeadingEscapeState : BaseState<EscapeAfterLeadingEscapeState>
{
    public override ParserState Id => ParserState.EscapeAfterLeadingEscape;
    
    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Escape, Instance)
            .Add(CsvTokens.FieldDelimiter, EndOfFieldState.Instance)
            .Add(CsvTokens.Quote, QuoteAfterLeadingEscapeState.Instance)
            .Add(CsvTokens.WhiteSpace, QuotedFieldClosingQuoteTrailingWhiteSpaceState.Instance)
            .Add(CsvTokens.Text, UnexpectedTokenState.Instance);
    }
}