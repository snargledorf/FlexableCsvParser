using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal abstract class BaseState
{
    public abstract ParserState Id { get; }

    public bool TryTransition(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState) =>
        TryGetNextState(token, out nextState) || TryGetDefault(out nextState);

    protected abstract bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState);

    public abstract bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState);
}

internal abstract class BaseState<T> : BaseState where T : BaseState, IStateMapProvider, IDefaultStateProvider, new()
{
    public static readonly T Instance = new();

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? nextState)
    {
        return T.StateMap.TryGetState(token, out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out BaseState? defaultState)
    {
        defaultState = T.DefaultState;
        return defaultState is not null;
    }
}