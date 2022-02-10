using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal partial class StateTransitionMapBuilder<TState, TInput> : IStateTransitionMapBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {

        private StateTransitionMapBuilder<TState, TInput>? parentBuilder;

        private StateTransitionMapBuilder<TState, TInput>? thenBuilder;

        private readonly List<Transition<TState, TInput>> transitions = new();

        public IStateTransitionMapBuilder<TState, TInput> Then => thenBuilder = new StateTransitionMapBuilder<TState, TInput>(State, this);

        public TState State { get; }

        public StateTransitionMapBuilder(TState state)
        {
            this.State = state;
        }

        private StateTransitionMapBuilder(TState state, StateTransitionMapBuilder<TState, TInput> stateMapBuilder)
            : this(state)
        {
            this.parentBuilder = stateMapBuilder;
        }

        public IStateTransitionMapBuilder<TState, TInput> When(TInput input, TState newState)
        {
            return When(i => i.Equals(input), newState);
        }

        public IStateTransitionMapBuilder<TState, TInput> When(Expression<Func<TInput, bool>> checkInputExpression, TState newState)
        {
            var transition = new Transition<TState, TInput>(checkInputExpression, newState);

            transitions.Add(transition);
            
            return this;
        }

        public void Default(TState newState)
        {
            When(_ => true, newState);
        }

        public Expression<CheckTransitionsDelegate<TState, TInput>> Build(LabelTarget returnTarget)
        {
            if (parentBuilder != null)
                return parentBuilder.Build(returnTarget);

            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());

            IEnumerable<Transition<TState, TInput>> transitions = this.transitions;

            List<Expression> transitionExpressions = new();

            foreach (var transition in transitions)
            {
                Expression transitionExpression = CreateExpressionForTransition(inputParam, outNewStateParam, transition, returnTarget);
                transitionExpressions.Add(transitionExpression);
            }


            StateTransitionMapBuilder<TState, TInput>? thenBuilder = this.thenBuilder;
            while (thenBuilder != null)
            {
                foreach (var transition in thenBuilder.transitions)
                {
                    Expression transitionExpression = CreateExpressionForTransition(inputParam, outNewStateParam, transition, returnTarget);
                    transitionExpressions.Add(transitionExpression);
                }

                thenBuilder = thenBuilder.thenBuilder;
            }

            BinaryExpression assignOutNewStateDefault = Expression.Assign(outNewStateParam, Expression.Constant(State));
            transitionExpressions.Add(assignOutNewStateDefault);

            GotoExpression returnFalse = Expression.Return(returnTarget, Expression.Constant(false));
            transitionExpressions.Add(returnFalse);

            transitionExpressions.Add(Expression.Label(returnTarget, Expression.Constant(false)));

            BlockExpression checkTransitionsBody = Expression.Block(typeof(bool), transitionExpressions);

            return Expression.Lambda<CheckTransitionsDelegate<TState, TInput>>(checkTransitionsBody, inputParam, outNewStateParam);

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