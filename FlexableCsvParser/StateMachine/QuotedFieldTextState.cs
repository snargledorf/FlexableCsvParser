using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTextState : BaseState<QuotedFieldTextState>
{
    public override ParserState Id => ParserState.QuotedFieldText;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = Instance;
        return true;
    }
    
    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
        lookupCollection
            .Add(CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance)
            .Add(CsvTokens.Escape, QuotedFieldEscapeState.Instance)
            .Add(CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance);
    }
}