using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal static class StateMachineTransitionMapExpressionFactory<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        public static Expression<TryTransitionDelegate<TState, TInput>> BuildTryTransitionExpression(IStateMachineTransitionMap<TState, TInput> map)
        {
            ParameterExpression stateParam = Expression.Parameter(typeof(TState));
            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());

            LabelTarget returnTarget = Expression.Label(typeof(bool));

            var switchCaseExpressions = map
                .Select(tm =>
                {
                    var transitionMapExpression = TransitionMapExpressionFactory<TState, TInput>.BuildTryGetValueExpression(tm, inputParam, outNewStateParam, returnTarget);
                    return Expression.SwitchCase(transitionMapExpression, Expression.Constant(tm.State));
                })
                .ToArray();

            SwitchExpression stateSwitch = Expression.Switch(stateParam, switchCaseExpressions);

            BlockExpression body = Expression.Block(
                typeof(bool),
                stateSwitch,
                Expression.Throw(Expression.Constant(new ArgumentException("Invalid state"))),
                Expression.Label(returnTarget, Expression.Constant(false)));

            return Expression.Lambda<TryTransitionDelegate<TState, TInput>>(body, stateParam, inputParam, outNewStateParam);
        }

        public static Expression<TryGetDefaultDelegate<TState, TInput>> BuildTryGetDefaultExpression(IStateMachineTransitionMap<TState, TInput> map)
        {
            ParameterExpression stateParam = Expression.Parameter(typeof(TState));
            ParameterExpression outNewStateParam = Expression.Parameter(typeof(TState).MakeByRefType());
            LabelTarget returnTarget = Expression.Label(typeof(bool));

            var switchCaseExpressions = map
                .Where(tm => tm.HasDefaultTransitionState)
                .Select(tm =>
                {
                    var getDefaultExpression = TransitionMapExpressionFactory<TState, TInput>.BuildGetDefaultExpression(tm, outNewStateParam, returnTarget);
                    return Expression.SwitchCase(getDefaultExpression, Expression.Constant(tm.State));
                })
                .ToArray();

            SwitchExpression stateSwitch = Expression.Switch(stateParam, switchCaseExpressions);

            BlockExpression body = Expression.Block(
                typeof(bool),
                stateSwitch,
                Expression.Label(returnTarget, Expression.Constant(false)));

            return Expression.Lambda<TryGetDefaultDelegate<TState, TInput>>(body, stateParam, outNewStateParam);
        }
    }
}
