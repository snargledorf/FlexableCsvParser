using System;
using System.Diagnostics.CodeAnalysis;

namespace FlexableCsvParser.StateMachine;

internal class UnexpectedTokenState : BaseState<UnexpectedTokenState>
{
    public override ParserState Id => ParserState.UnexpectedToken;

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        throw new NotSupportedException("Unexpected Token");
    }

    protected override void AddStates(IStateLookupCollection lookupCollection)
    {
    }
}