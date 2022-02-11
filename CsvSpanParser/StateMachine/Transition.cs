using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal class Transition<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        public Transition(Expression conditions, TState newState)
        {
            Condition = conditions;
            NewState = newState;
        }

        public Expression Condition { get; }

        public TState NewState { get; set; }
    }

}