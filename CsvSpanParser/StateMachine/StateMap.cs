namespace CsvSpanParser.StateMachine
{
    internal class StateMap<TState, TInput> : IStateMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly Transition<TState, TInput>[] transitions;

        public StateMap(TState state, Transition<TState, TInput>[] transitions)
        {
            State = state;

            this.transitions = transitions;
        }

        public TState State { get; }

        public bool TryGetNewState(TInput input, out TState? newState)
        {
            foreach (var transition in transitions)
            {
                if (transition.CheckInput(input))
                {
                    newState = transition.NewState;
                    return true;
                }
            }

            newState = State;
            return false;
        }
    }
}