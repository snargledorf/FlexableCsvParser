using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    public interface ITransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        TState State { get; }

        IStateMachineTransitionMapBuilder<TState, TInput> StateMachineTransitionMapBuilder { get; }

        ITransitionMapBuilder<TState, TInput> RootBuilder { get; }

        ITransitionMapBuilder<TState, TInput> Else { get; }

        ITransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> condition, TState newState);

        ITransitionMapBuilder<TState, TInput> When(TInput input, TState newState);

        ITransitionMapBuilder<TState, TInput> GotoWhen(TState newState, params TInput[] input);

        ITransitionMapBuilder<TState, TInput> Default(TState state);
    }
}