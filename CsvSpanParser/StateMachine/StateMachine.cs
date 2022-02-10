namespace CsvSpanParser.StateMachine
{
    internal sealed class StateMachine<TState, TInput> : IStateMachine<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly IStateMapsCollection<TState, TInput> statesMap;

        public StateMachine(Action<IStateMapsCollectionBuilder<TState, TInput>> buildStates)
        {
            var statesMapBuilder = new StateMapsCollectionBuilder<TState, TInput>();
            buildStates(statesMapBuilder);
            statesMap = statesMapBuilder.Build();
        }

        public bool TryTransition(TState state, TInput input, out TState? newState)
        {
            if (statesMap.TryGetMapForState(state, out IStateMap<TState, TInput> map))
                return map.TryGetNewState(input, out newState);
            
            newState = default;
            return false;
        }
    }
}
