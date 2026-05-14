using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTextState : BaseState<QuotedFieldTextState>, IStateMapProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.QuotedFieldText;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = Instance;
        return true;
    }
}