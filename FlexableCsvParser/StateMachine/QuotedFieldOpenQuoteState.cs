using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldOpenQuoteState : BaseState<QuotedFieldOpenQuoteState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldLeadingWhiteSpaceState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.QuotedFieldOpenQuote;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}