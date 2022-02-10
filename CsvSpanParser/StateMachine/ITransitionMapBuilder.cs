using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    public interface ITransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        TState State { get; }

        ITransitionMapBuilder<TState, TInput> RootBuilder { get; }

        ITransitionMapBuilder<TState, TInput> Else { get; }

        ITransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> condition, TState newState);
        ITransitionMapBuilder<TState, TInput> When(TInput input, TState newState);

        void Default(TState state);
    }
}