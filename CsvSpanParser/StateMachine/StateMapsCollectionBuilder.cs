namespace CsvSpanParser.StateMachine
{
    internal class StateMapsCollectionBuilder<TState, TInput> : IStateMapsCollectionBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        private readonly List<StateMapBuilder<TState, TInput>> stateMapBuilders = new();

        public IStateMapsCollection<TState, TInput> Build()
        {
            return new StateMapsCollection<TState, TInput>(stateMapBuilders.Select(builder => builder.Build()));
        }

        public IStateMapBuilder<TState, TInput> From(TState state)
        {
            StateMapBuilder<TState, TInput> stateMapBuilder = new StateMapBuilder<TState, TInput>(state);
            stateMapBuilders.Add(stateMapBuilder);
            return stateMapBuilder;
        }
    }
}