namespace CsvSpanParser.StateMachine
{
    internal class StateMachineTransitionMapBuilder<TState, TInput> : IStateMachineTransitionMapBuilder<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly List<TransitionMapBuilder<TState, TInput>> transitionMapBuilders = new();

        public IStateMachineTransitionMap<TState, TInput> Build()
        {
            return new StateMachineTransitionMap<TState, TInput>(transitionMapBuilders.Select(mp => mp.Build()));
        }

        public ITransitionMapBuilder<TState, TInput> From(TState state)
        {
            TransitionMapBuilder<TState, TInput> stateMapBuilder = new(state, this);
            transitionMapBuilders.Add(stateMapBuilder);
            return stateMapBuilder;
        }
    }
}