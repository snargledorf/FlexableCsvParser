namespace CsvSpanParser.StateMachine
{
    public interface IStateMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        TState State { get; }

        bool TryGetNewState(TInput input, out TState? newState);
    }
}