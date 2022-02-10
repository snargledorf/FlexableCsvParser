namespace CsvSpanParser.StateMachine
{
    public interface IStateMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        bool TryGetNewState(TInput input, out TState? newState);
    }
}