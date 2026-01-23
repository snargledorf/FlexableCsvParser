using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal abstract class BaseState<T> : IState where T : BaseState<T>, IState, new()
{
    public abstract ParserState Id { get; }

    public bool TryTransition(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState) =>
        TryGetNextState(token, out nextState) || TryGetDefault(out nextState);

    protected abstract bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState);

    public abstract bool TryGetDefault([NotNullWhen(true)] out IState? defaultState);

    public static T Instance => field ??= new T();
}