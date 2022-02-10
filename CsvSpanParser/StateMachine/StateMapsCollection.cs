namespace CsvSpanParser.StateMachine
{
    internal class StateMapsCollection<TState, TInput> : IStateMapsCollection<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private IDictionary<TState, IStateMap<TState, TInput>> stateMapLookup;

        public StateMapsCollection(IEnumerable<IStateMap<TState, TInput>> enumerable)
        {
            stateMapLookup = enumerable.ToDictionary(sm => sm.State, sm => sm);
        }

        public bool TryGetMapForState(TState state, out IStateMap<TState, TInput> map)
        {
            return stateMapLookup.TryGetValue(state, out map);
        }
    }
}