using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal record Transition<TState, TInput>(Expression<Func<TInput, bool>> CheckInput, TState NewState)
        where TState : notnull
        where TInput : notnull;

}