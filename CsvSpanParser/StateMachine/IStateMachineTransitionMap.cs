namespace CsvSpanParser.StateMachine
{
    internal interface IStateMachineTransitionMap<TState, TInput> : IEnumerable<ITransitionMap<TState, TInput>>
        where TState : notnull
        where TInput : notnull
    {
    }
}