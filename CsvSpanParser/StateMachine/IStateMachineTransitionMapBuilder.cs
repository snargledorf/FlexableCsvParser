namespace CsvSpanParser.StateMachine
{
    public interface IStateMachineTransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {        
        ITransitionMapBuilder<TState, TInput> From(TState state);
    }
}