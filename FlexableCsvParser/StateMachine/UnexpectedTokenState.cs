using System;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class UnexpectedTokenState : BaseState<UnexpectedTokenState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap => throw new NotSupportedException("Unexpected Token");

    public static BaseState? DefaultState => throw new NotSupportedException("Unexpected Token");

    public override ParserState Id => ParserState.UnexpectedToken;
}