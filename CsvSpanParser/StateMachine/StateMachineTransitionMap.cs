using System.Collections;

namespace CsvSpanParser.StateMachine
{
    internal class StateMachineTransitionMap<TState, TInput> : IStateMachineTransitionMap<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        private IEnumerable<ITransitionMap<TState, TInput>> transitionMaps;

        public StateMachineTransitionMap(IEnumerable<ITransitionMap<TState, TInput>> enumerable)
        {
            transitionMaps = enumerable;
        }

        public IEnumerator<ITransitionMap<TState, TInput>> GetEnumerator()
        {
            return transitionMaps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}