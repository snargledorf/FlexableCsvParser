using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTextState : BaseState<UnquotedFieldTextState>
{
    private static readonly StateMap StateMap =
        new StateMapBuilder
        {
            { CsvTokens.WhiteSpace, UnquotedFieldTrailingWhiteSpaceState.Instance },
            { CsvTokens.FieldDelimiter, EndOfFieldState.Instance },
            { CsvTokens.EndOfRecord, EndOfRecordState.Instance }
        }.Build();
    
    public override ParserState Id => ParserState.UnquotedFieldText;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return StateMap.TryGetState(token, out nextState) || TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = this;
        return true;
    }
}