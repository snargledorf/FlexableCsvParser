using System;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnexpectedTokenState : BaseState<UnexpectedTokenState>, IStateMapProvider
{
    public static StateMap StateMap => throw new NotSupportedException("Unexpected Token");

    public override ParserState Id => ParserState.UnexpectedToken;

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        throw new NotSupportedException("Unexpected Token");
    }
}