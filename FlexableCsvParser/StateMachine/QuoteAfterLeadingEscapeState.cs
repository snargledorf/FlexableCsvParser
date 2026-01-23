using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuoteAfterLeadingEscapeState : BaseState<QuoteAfterLeadingEscapeState>
{
    public override ParserState Id => ParserState.QuoteAfterLeadingEscape;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance)
            .Add(CsvTokens.Quote, UnexpectedTokenState.Instance)
            .Add(CsvTokens.Escape, UnexpectedTokenState.Instance);
    }
}