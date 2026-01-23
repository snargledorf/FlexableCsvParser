using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldOpenQuoteState : BaseState<QuotedFieldOpenQuoteState>
{
    public override ParserState Id => ParserState.QuotedFieldOpenQuote;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance)
            .Add(CsvTokens.Escape, QuotedFieldEscapeState.Instance)
            .Add(CsvTokens.WhiteSpace, QuotedFieldLeadingWhiteSpaceState.Instance);
    }
}