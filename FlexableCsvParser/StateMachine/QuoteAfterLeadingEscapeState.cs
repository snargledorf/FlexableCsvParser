using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuoteAfterLeadingEscapeState : BaseState<QuoteAfterLeadingEscapeState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuoteAfterLeadingEscape;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}