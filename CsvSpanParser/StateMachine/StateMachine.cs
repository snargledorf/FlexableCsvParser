namespace CsvSpanParser.StateMachine
{
    internal sealed class StateMachine<TState, TInput> : IStateMachine<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private TryTransitionDelegate<TState, TInput> tryTransitions;

        public StateMachine(Action<IStateMachineConfigBuilder<TState, TInput>> buildStates)
        {
            var stateMachineConfig = new StateMachineConfigBuilder<TState, TInput>();
            buildStates(stateMachineConfig);
            tryTransitions = stateMachineConfig.Build();
        }

        public bool TryTransition(TState state, TInput input, out TState? newState)
        {
            return tryTransitions(state, input, out newState);
        }
    }
}
