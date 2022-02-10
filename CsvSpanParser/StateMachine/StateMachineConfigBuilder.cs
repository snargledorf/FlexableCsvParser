using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal class StateMachineConfigBuilder<TState, TInput> : IStateMachineConfigBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        private readonly List<StateTransitionMapBuilder<TState, TInput>> stateMapBuilders = new();

        public TryTransitionDelegate<TState, TInput> Build()
        {
            ParameterExpression stateParam = Expression.Parameter(typeof(TState));
            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());

            LabelTarget returnTarget = Expression.Label(typeof(bool));

            var switchCaseExpressions = stateMapBuilders
                .Select(builder => Expression.SwitchCase(Expression.Return(returnTarget, Expression.Invoke(builder.Build(returnTarget), inputParam, outNewStateParam)), Expression.Constant(builder.State)))
                .ToArray();

            SwitchExpression stateSwitch = Expression.Switch(stateParam, Expression.Throw(Expression.Constant(new ArgumentException("Invalid state"))), switchCaseExpressions);

            Expression<TryTransitionDelegate<TState, TInput>> expression = Expression.Lambda<TryTransitionDelegate<TState, TInput>>(Expression.Block(typeof(bool), stateSwitch, Expression.Label(returnTarget, Expression.Constant(false))), stateParam, inputParam, outNewStateParam);
            return expression.Compile();
        }

        public IStateTransitionMapBuilder<TState, TInput> From(TState state)
        {
            StateTransitionMapBuilder<TState, TInput> stateMapBuilder = new(state);
            stateMapBuilders.Add(stateMapBuilder);
            return stateMapBuilder;
        }
    }
}