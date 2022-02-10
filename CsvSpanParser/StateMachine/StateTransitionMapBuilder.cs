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

        public BlockExpression Build(ParameterExpression inputParam, ParameterExpression outNewStateParam, LabelTarget returnTarget)
        {
            LabelTarget localReturnTarget = Expression.Label(typeof(bool));

            List<Expression> transitionExpressions = new();

            StateTransitionMapBuilder<TState, TInput>? currentBuilder = RootBuilder;
            while (currentBuilder != null)
            {
                var contstantSwitchCases = currentBuilder.transitions
                    .Where(t => t.CheckInput.NodeType == ExpressionType.Constant)
                    .Select(t =>
                        Expression.SwitchCase(
                            Expression.Block(
                                Expression.Assign(outNewStateParam, Expression.Constant(t.NewState)),
                                Expression.Return(returnTarget, Expression.Constant(true))
                            ),
                            t.CheckInput
                        )
                    );

                if (contstantSwitchCases.Any())
                {
                    SwitchCase[] cases = contstantSwitchCases.ToArray();
                    SwitchExpression constantsSwitch = Expression.Switch(inputParam, cases);
                    transitionExpressions.Add(constantsSwitch);
                }

                var delegateCheckInputs = currentBuilder.transitions
                    .Where(t => t.CheckInput.NodeType != ExpressionType.Constant)
                    .Select(t =>
                        Expression.IfThen(
                            Expression.Invoke(t.CheckInput, inputParam),
                            Expression.Block(
                                Expression.Assign(outNewStateParam, Expression.Constant(t.NewState)),
                                Expression.Return(returnTarget, Expression.Constant(true))
                            )
                        )
                    );

                if (delegateCheckInputs.Any())
                {
                    transitionExpressions.AddRange(delegateCheckInputs);
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
                transitionExpressions.Add(Expression.Assign(outNewStateParam, Expression.Constant(State)));
                transitionExpressions.Add(Expression.Return(returnTarget, Expression.Constant(false)));
            }

            BlockExpression checkTransitionsBody = Expression.Block(transitionExpressions);
            return checkTransitionsBody;
        }
    }

}