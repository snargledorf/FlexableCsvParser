namespace CsvSpanParser.StateMachine
{
    internal delegate bool TryTransitionDelegate<TState, TInput>(TState state, TInput input, out TState newState)
    where TState : notnull
    where TInput : notnull;
}
