using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuoteAfterLeadingEscapeState : BaseState<QuoteAfterLeadingEscapeState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Quote, UnexpectedTokenState.Instance },
            { CsvTokens.Escape, UnexpectedTokenState.Instance }
        }.Build();

    public static BaseState? DefaultState => QuotedFieldTextState.Instance;

    public override ParserState Id => ParserState.QuoteAfterLeadingEscape;
}