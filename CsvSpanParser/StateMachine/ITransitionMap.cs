using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal interface ITransitionMap<TState, TInput> : IEnumerable<Transition<TState, TInput>>
        where TState : notnull
        where TInput : notnull
    {
        ITransitionMap<TState, TInput> Root { get; }
        ITransitionMap<TState, TInput>? Else { get; }
        bool HasDefaultTransitionState { get; }
        TState DefaultTransitionState { get; }
        TState State { get; }
    }
}