using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal partial class StateTransitionMapBuilder<TState, TInput> : IStateTransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        private readonly List<Transition<TState, TInput>> transitions = new();

        private StateTransitionMapBuilder<TState, TInput>? parentBuilder;

        private StateTransitionMapBuilder<TState, TInput>? thenBuilder;

        private ConstantExpression? defaultStateExpression;

        public StateTransitionMapBuilder(TState state)
        {
            State = state;
        }

        private StateTransitionMapBuilder(TState state, StateTransitionMapBuilder<TState, TInput> parentBuilder)
            : this(state)
        {
            this.parentBuilder = parentBuilder;
        }

        public IStateTransitionMapBuilder<TState, TInput> Then => thenBuilder ??= new StateTransitionMapBuilder<TState, TInput>(State, this);

        public TState State { get; }

        IStateTransitionMapBuilder<TState, TInput> IStateTransitionMapBuilder<TState, TInput>.RootBuilder => RootBuilder;

        public StateTransitionMapBuilder<TState, TInput> RootBuilder => parentBuilder?.RootBuilder ?? this;

        public IStateTransitionMapBuilder<TState, TInput> When(TInput input, TState newState)
        {
            AddTransitionCheck(Expression.Constant(input), newState);
            return this;
        }

        public IStateTransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> checkInputExpression, TState newState)
        {
            AddTransitionCheck(checkInputExpression, newState);
            return this;
        }

        private void AddTransitionCheck(Expression checkInput, TState newState)
        {
            var transition = new Transition<TState, TInput>(checkInput, newState);
            transitions.Add(transition);
        }

        public void Default(TState newState)
        {
            RootBuilder.defaultStateExpression = Expression.Constant(newState);
        }

        public Expression<CheckTransitionsDelegate<TState, TInput>> Build(LabelTarget returnTarget)
        {
            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());

            List<Expression> transitionExpressions = new();

            StateTransitionMapBuilder<TState, TInput>? currentBuilder = RootBuilder;
            while (currentBuilder != null)
            {
                foreach (var transition in currentBuilder.transitions)
                {
                    Expression transitionExpression = CreateExpressionForTransition(inputParam, outNewStateParam, transition, returnTarget);
                    transitionExpressions.Add(transitionExpression);
                }

                currentBuilder = currentBuilder.thenBuilder;
            }

            ConstantExpression? defaultStateExpression = RootBuilder.defaultStateExpression;
            if (defaultStateExpression != null)
            {
                transitionExpressions.Add(Expression.Assign(outNewStateParam, defaultStateExpression));
                transitionExpressions.Add(Expression.Return(returnTarget, Expression.Constant(true)));
            }
            else
            {
                BinaryExpression assignOutNewStateDefault = Expression.Assign(outNewStateParam, Expression.Constant(State));
                transitionExpressions.Add(assignOutNewStateDefault);

                GotoExpression returnFalse = Expression.Return(returnTarget, Expression.Constant(false));
                transitionExpressions.Add(returnFalse);
            }

            transitionExpressions.Add(Expression.Label(returnTarget, Expression.Constant(false)));

            BlockExpression checkTransitionsBody = Expression.Block(typeof(bool), transitionExpressions);

            return Expression.Lambda<CheckTransitionsDelegate<TState, TInput>>(checkTransitionsBody, inputParam, outNewStateParam);

            static Expression CreateExpressionForTransition(ParameterExpression inputParam, ParameterExpression outNewStateParam, Transition<TState, TInput> transition, LabelTarget returnTarget)
            {
                var checkInputExpression = transition.CheckInput;
                if (checkInputExpression.NodeType == ExpressionType.Constant)
                {
                    if (checkInputExpression.Type == typeof(TInput))
                        checkInputExpression = Expression.Equal(inputParam, checkInputExpression);
                    else
                        throw new InvalidOperationException("Unexpected input check expression type: " + checkInputExpression.Type);
                }
                else
                {
                    checkInputExpression = Expression.Invoke(checkInputExpression, inputParam);
                }

                ConstantExpression newStateValue = Expression.Constant(transition.NewState);
                BinaryExpression assignOutNewState = Expression.Assign(outNewStateParam, newStateValue);

                BlockExpression ifBody =
                    Expression.Block(
                        assignOutNewState,
                        Expression.Return(returnTarget, Expression.Constant(true)));

                return Expression.IfThen(checkInputExpression, ifBody);
            }
        }
    }

}