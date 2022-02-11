using System.Linq.Expressions;

namespace CsvSpanParser.StateMachine
{
    internal static class TransitionMapExpressionFactory<TState, TInput>
        where TState : notnull
        where TInput : notnull
    {
        internal static BlockExpression BuildExpression(ITransitionMap<TState, TInput> map, ParameterExpression inputParam, ParameterExpression outNewStateParam, LabelTarget returnTarget)
        {
            List<Expression> transitionExpressions = new();

            ITransitionMap<TState, TInput>? currentMap = map.Root;
            while (currentMap != null)
            {
                var contstantSwitchCases = currentMap
                    .Where(t => t.Condition.NodeType == ExpressionType.Constant)
                    .Select(t =>
                        Expression.SwitchCase(
                            Expression.Block(
                                Expression.Assign(outNewStateParam, Expression.Constant(t.NewState)),
                                Expression.Return(returnTarget, Expression.Constant(true))
                            ),
                            t.Condition
                        )
                    );

                if (contstantSwitchCases.Any())
                {
                    SwitchCase[] cases = contstantSwitchCases.ToArray();
                    SwitchExpression constantsSwitch = Expression.Switch(inputParam, cases);
                    transitionExpressions.Add(constantsSwitch);
                }

                var delegateCheckInputs = currentMap
                    .Where(t => t.Condition is Expression<Func<TInput, bool>>)
                    .Select(t => Expression.IfThen(
                            Expression.Invoke(t.Condition, inputParam),
                            Expression.Block(
                                Expression.Assign(outNewStateParam, Expression.Constant(t.NewState)),
                                Expression.Return(returnTarget, Expression.Constant(true))
                            )
                        ));

                if (delegateCheckInputs.Any())
                {
                    transitionExpressions.AddRange(delegateCheckInputs);
                }

                currentMap = currentMap.Else;
            }

            if (map.Root.HasDefaultTransitionState)
            {
                transitionExpressions.Add(Expression.Assign(outNewStateParam, Expression.Constant(map.Root.DefaultTransitionState)));
                transitionExpressions.Add(Expression.Return(returnTarget, Expression.Constant(true)));
            }
            else
            {
                transitionExpressions.Add(Expression.Assign(outNewStateParam, Expression.Constant(map.State)));
                transitionExpressions.Add(Expression.Return(returnTarget, Expression.Constant(false)));
            }

            return Expression.Block(transitionExpressions);
        }
    }
}
