using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTrailingWhiteSpaceState : BaseState<QuotedFieldTrailingWhiteSpaceState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();

    public override ParserState Id => ParserState.QuotedFieldTrailingWhiteSpace;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = QuotedFieldTextState.Instance;
        return true;
    }
}