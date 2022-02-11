namespace CsvSpanParser.StateMachine
{
    internal class StateMachineTransitionMapBuilder<TState, TInput> : IStateMachineTransitionMapBuilder<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly Dictionary<TState, TransitionMapBuilder<TState, TInput>> transitionMapBuilders = new();

        public IStateMachineTransitionMap<TState, TInput> Build()
        {
            return new StateMachineTransitionMap<TState, TInput>(transitionMapBuilders.Values.Select(mp => mp.Build()));
        }

        public ITransitionMapBuilder<TState, TInput> From(TState state)
        {
            return transitionMapBuilders.ContainsKey(state)
                ? transitionMapBuilders[state]
                : transitionMapBuilders[state] = new(state, this);
        }
    }
}