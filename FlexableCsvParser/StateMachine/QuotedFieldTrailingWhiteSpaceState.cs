using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTrailingWhiteSpaceState : BaseState<QuotedFieldTrailingWhiteSpaceState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();

    public static BaseState? DefaultState => QuotedFieldTextState.Instance;

    public override ParserState Id => ParserState.QuotedFieldTrailingWhiteSpace;
}