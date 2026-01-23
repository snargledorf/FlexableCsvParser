using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldEscapeState : BaseState<QuotedFieldEscapeState>
{
    public override ParserState Id => ParserState.QuotedFieldEscape;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance)
            .Add(CsvTokens.Escape, Instance);
    }
}