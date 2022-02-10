namespace CsvSpanParser.StateMachine
{
    public interface IStateMapsCollection<TState, TInput>
    {
        bool TryGetMapForState(TState state, out IStateMap<TState, TInput>? map);
    }
}