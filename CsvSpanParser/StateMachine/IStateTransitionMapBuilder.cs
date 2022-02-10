using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    public interface IStateTransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        TState State { get; }

        IStateTransitionMapBuilder<TState, TInput> Then { get; }

        IStateTransitionMapBuilder<TState, TInput> When(TInput input, TState newState);
        IStateTransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> checkInputExpression, TState newState);
        
        void Default(TState state);
    }
}