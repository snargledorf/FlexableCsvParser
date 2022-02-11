﻿using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal class TransitionMapBuilder<TState, TInput> : ITransitionMapBuilder<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        internal readonly List<Transition<TState, TInput>> transitions = new();

        private TransitionMapBuilder<TState, TInput>? parentBuilder;

        internal TransitionMapBuilder<TState, TInput>? elseBuilder;

        public TransitionMapBuilder(TState state, IStateMachineTransitionMapBuilder<TState, TInput> stateMachineTransitionMapBuilder)
        {
            State = DefaultTransitionState = state;
            StateMachineTransitionMapBuilder = stateMachineTransitionMapBuilder;
        }

        private TransitionMapBuilder(TState state, TransitionMapBuilder<TState, TInput> parentBuilder, IStateMachineTransitionMapBuilder<TState, TInput> stateMachineTransitionMapBuilder)
            : this(state, stateMachineTransitionMapBuilder)
        {
            this.parentBuilder = parentBuilder;
        }

        public ITransitionMapBuilder<TState, TInput> Else => elseBuilder ??= new TransitionMapBuilder<TState, TInput>(State, this, StateMachineTransitionMapBuilder);

        public TState State { get; }

        public TState DefaultTransitionState { get; private set; }

        public bool HasDefaultTransitionState => !EqualityComparer<TState>.Default.Equals(DefaultTransitionState, State);

        ITransitionMapBuilder<TState, TInput> ITransitionMapBuilder<TState, TInput>.RootBuilder => RootBuilder;

        public TransitionMapBuilder<TState, TInput> RootBuilder => parentBuilder?.RootBuilder ?? this;

        public IStateMachineTransitionMapBuilder<TState, TInput> StateMachineTransitionMapBuilder { get; }

        public ITransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> condition, TState newState)
        {
            transitions.Add(new Transition<TState, TInput>(condition, newState));
            return this;
        }

        public ITransitionMapBuilder<TState, TInput> When(TInput input, TState newState)
        {
            transitions.Add(new Transition<TState, TInput>(Expression.Constant(input), newState));
            return this;
        }

        public ITransitionMapBuilder<TState, TInput> GotoWhen(TState newState, params TInput[] inputs)
        {
            foreach (var input in inputs)
                When(input, newState);

            return this;
        }

        public ITransitionMapBuilder<TState, TInput> Default(TState newState)
        {
            RootBuilder.DefaultTransitionState = newState;
            return this;
        }

        public TransitionMap<TState, TInput> Build()
        {
            return RootBuilder.BuildRecursive();
        }

        private TransitionMap<TState, TInput> BuildRecursive()
        {
            if (elseBuilder != null)
            {
                TransitionMap<TState, TInput> elseMap = elseBuilder.BuildRecursive();
                return new TransitionMap<TState, TInput>(State, transitions, elseMap, DefaultTransitionState);
            }

            return new TransitionMap<TState, TInput>(State, transitions, DefaultTransitionState);
        }
    }

}