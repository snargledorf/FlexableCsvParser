namespace CsvSpanParser.StateMachine
{

    internal delegate bool TryGetDefaultDelegate<TState, TInput>(TState state, out TState newState)
        where TState : notnull
        where TInput : notnull;
}
