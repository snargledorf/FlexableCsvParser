namespace CsvSpanParser.StateMachine
{
    public interface IStateMachineConfigBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {        
        IStateTransitionMapBuilder<TState, TInput> From(TState state);
    }
}