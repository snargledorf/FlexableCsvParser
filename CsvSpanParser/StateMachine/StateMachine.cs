using System.Runtime.CompilerServices;

namespace CsvSpanParser.StateMachine
{
    internal sealed class StateMachine<TState, TInput> : IStateMachine<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private TryTransitionDelegate<TState, TInput> tryTransitions;

        public StateMachine(Action<IStateMachineTransitionMapBuilder<TState, TInput>> buildStates)
        {
            var stateMachineConfig = new StateMachineTransitionMapBuilder<TState, TInput>();
            buildStates(stateMachineConfig);
            IStateMachineTransitionMap<TState, TInput> stateMachineTransitionMap = stateMachineConfig.Build();
            tryTransitions = StateMachineTransitionMapExpressionFactory<TState, TInput>.BuildExpression(stateMachineTransitionMap).Compile();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTransition(TState state, TInput input, out TState? newState)
        {
            return tryTransitions(state, input, out newState);
        }
    }
}
