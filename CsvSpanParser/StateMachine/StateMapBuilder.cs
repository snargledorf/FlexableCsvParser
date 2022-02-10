using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal partial class StateMapBuilder<TState, TInput> : IStateMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        private readonly TState state;

        private StateMapBuilder<TState, TInput>? parentBuilder;

        private StateMapBuilder<TState, TInput>? thenBuilder;

        private readonly List<Transition<TState, TInput>> transitions = new();

        public IStateMapBuilder<TState, TInput> Then => thenBuilder = new StateMapBuilder<TState, TInput>(state, this);

        public StateMapBuilder(TState state)
        {
            this.state = state;
        }

        private StateMapBuilder(TState state, StateMapBuilder<TState, TInput> stateMapBuilder)
            : this(state)
        {
            this.parentBuilder = stateMapBuilder;
        }

        public IStateMapBuilder<TState, TInput> When(TInput input, TState newState)
        {
            return When(i => i.Equals(input), newState);
        }

        public IStateMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> checkInputExpression, TState newState)
        {
            Func<TInput, bool> checkInput = checkInputExpression.Compile();
            var transition = new Transition<TState, TInput>(checkInput, newState);

            transitions.Add(transition);
            
            return this;
        }

        public void Default(TState newState)
        {
            When(_ => true, newState);
        }

        public IStateMap<TState, TInput> Build()
        {
            if (parentBuilder != null)
                return parentBuilder.Build();

            IEnumerable<Transition<TState, TInput>> transitions = this.transitions;

            StateMapBuilder<TState, TInput>? thenBuilder = this.thenBuilder;
            while (thenBuilder != null)
            {
                transitions = transitions.Concat(thenBuilder.transitions);
                thenBuilder = thenBuilder.thenBuilder;
            }

            return new StateMap<TState, TInput>(state, transitions.ToArray());
        }
    }

}