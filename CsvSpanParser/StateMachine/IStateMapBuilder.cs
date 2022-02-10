using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    public interface IStateMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        IStateMapBuilder<TState, TInput> Then { get; }

        IStateMapBuilder<TState, TInput> When(TInput input, TState newState);
        IStateMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> checkInputExpression, TState newState);
        
        void Default(TState state);

        IStateMap<TState, TInput> Build();
    }
}