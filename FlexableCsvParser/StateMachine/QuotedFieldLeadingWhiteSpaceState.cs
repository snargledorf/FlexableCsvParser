using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldLeadingWhiteSpaceState : BaseState<QuotedFieldLeadingWhiteSpaceState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuotedFieldLeadingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}