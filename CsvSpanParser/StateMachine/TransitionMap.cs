using System.Collections;

namespace CsvSpanParser.StateMachine
{
    internal class TransitionMap<TState, TInput> : ITransitionMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private readonly IEnumerable<Transition<TState, TInput>> transitions;
        private TransitionMap<TState, TInput>? parentMap;

        public TransitionMap(
            TState state,
            IEnumerable<Transition<TState, TInput>> transitions,
            TState defaultTransitionState)
        {
            this.transitions = transitions;

            State = state;
            DefaultTransitionState = defaultTransitionState;
        }

        public TransitionMap(
            TState state,
            IEnumerable<Transition<TState, TInput>> transitions,
            TransitionMap<TState, TInput>? elseMap,
            TState defaultTransitionState)
            : this(state, transitions, defaultTransitionState)
        {
            elseMap?.SetParent(this);
            Else = elseMap;
        }

        private void SetParent(TransitionMap<TState, TInput> parentMap)
        {
            this.parentMap = parentMap;
        }

        public ITransitionMap<TState, TInput> Root => parentMap?.Root ?? this;

        public ITransitionMap<TState, TInput>? Else { get; }

        public bool HasDefaultTransitionState => !EqualityComparer<TState>.Default.Equals(DefaultTransitionState, State);

        public TState State { get; }

        public TState DefaultTransitionState { get; }

        public IEnumerator<Transition<TState, TInput>> GetEnumerator()
        {
            return transitions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}