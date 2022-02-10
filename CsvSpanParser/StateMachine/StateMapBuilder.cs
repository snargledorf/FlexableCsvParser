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
            var transition = new Transition<TState, TInput>(checkInputExpression, newState);

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

            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());

            IEnumerable<Transition<TState, TInput>> transitions = this.transitions;

            List<Expression> transitionExpressions = new();

            LabelTarget returnTarget = Expression.Label(typeof(bool));

            foreach (var transition in transitions)
            {
                Expression transitionExpression = CreateExpressionForTransition(inputParam, outNewStateParam, transition, returnTarget);
                transitionExpressions.Add(transitionExpression);
            }


            StateMapBuilder<TState, TInput>? thenBuilder = this.thenBuilder;
            while (thenBuilder != null)
            {
                foreach (var transition in thenBuilder.transitions)
                {
                    Expression transitionExpression = CreateExpressionForTransition(inputParam, outNewStateParam, transition, returnTarget);
                    transitionExpressions.Add(transitionExpression);
                }

                thenBuilder = thenBuilder.thenBuilder;
            }

            BinaryExpression assignOutNewStateDefault = Expression.Assign(outNewStateParam, Expression.Constant(state));
            transitionExpressions.Add(assignOutNewStateDefault);

            GotoExpression returnFalse = Expression.Return(returnTarget, Expression.Constant(false));
            transitionExpressions.Add(returnFalse);

            transitionExpressions.Add(Expression.Label(returnTarget, Expression.Constant(false)));

            BlockExpression checkTransitionsBody = Expression.Block(typeof(bool), transitionExpressions);

            CheckTransitionsDelegate<TState, TInput> checkTransitions =
                Expression.Lambda<CheckTransitionsDelegate<TState, TInput>>(checkTransitionsBody, inputParam, outNewStateParam).Compile();

            return new StateMap<TState, TInput>(state, checkTransitions);

            static Expression CreateExpressionForTransition(ParameterExpression inputParam, ParameterExpression outNewStateParam, Transition<TState, TInput> transition, LabelTarget returnTarget)
            {
                InvocationExpression checkInputInvoke = Expression.Invoke(transition.CheckInput, inputParam);
                ConstantExpression newStateValue = Expression.Constant(transition.NewState);
                BinaryExpression assignOutNewState = Expression.Assign(outNewStateParam, newStateValue);

                BlockExpression ifBody =
                    Expression.Block(
                        assignOutNewState,
                        Expression.Return(returnTarget, Expression.Constant(true)));

                return Expression.IfThen(checkInputInvoke, ifBody);
            }
        }
    }

}