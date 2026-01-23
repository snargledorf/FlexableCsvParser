using System;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnexpectedTokenState : BaseState<UnexpectedTokenState>
{
    public override ParserState Id => ParserState.UnexpectedToken;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        throw new NotSupportedException("Unexpected Token");
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        throw new NotSupportedException("Unexpected Token");
    }
}