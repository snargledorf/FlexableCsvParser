namespace CsvSpanParser.StateMachine
{
    internal interface IStateMachine<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        bool TryTransition(TState state, TInput input, out TState? newState);
    }
}