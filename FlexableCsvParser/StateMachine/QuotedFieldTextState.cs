using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldTextState : BaseState<QuotedFieldTextState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap { get; } =
        new StateMapBuilder
        {
            { CsvTokens.Quote, QuotedFieldClosingQuoteState.Instance },
            { CsvTokens.WhiteSpace, QuotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.Escape, QuotedFieldEscapeState.Instance }
        }.Build();

    public static BaseState? DefaultState => Instance;
    
    public override ParserState Id => ParserState.QuotedFieldText;
}