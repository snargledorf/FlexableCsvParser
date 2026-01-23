using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnquotedFieldTextState : BaseState<UnquotedFieldTextState>
{
    public override ParserState Id => ParserState.UnquotedFieldText;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.FieldDelimiter)
        {
            nextState = EndOfFieldState.Instance;
            return true;
        }

        if (token == CsvTokens.EndOfRecord)
        {
            nextState = EndOfRecordState.Instance;
            return true;
        }

        if (token == CsvTokens.WhiteSpace)
        {
            nextState = UnquotedFieldTrailingWhiteSpaceState.Instance;
            return true;
        }
        
        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = Instance;
        return true;
    }
}