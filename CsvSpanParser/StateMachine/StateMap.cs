namespace CsvSpanParser.StateMachine
{
    internal class StateMap<TState, TInput> : IStateMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly CheckTransitionsDelegate<TState, TInput> checkTransitionsDelegate;

        public StateMap(TState state, CheckTransitionsDelegate<TState, TInput> checkTransitionsDelegate)
        {
            State = state;
            this.checkTransitionsDelegate = checkTransitionsDelegate;
        }

        public TState State { get; }

        public bool TryGetNewState(TInput input, out TState? newState)
        {
            return checkTransitionsDelegate(input, out newState);
        }
    }
}