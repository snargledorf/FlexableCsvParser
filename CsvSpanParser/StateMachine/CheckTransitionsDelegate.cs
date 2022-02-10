namespace CsvSpanParser.StateMachine
{
    internal delegate bool CheckTransitionsDelegate<TState, TInput>(TInput input, out TState newState)
        where TState : notnull
        where TInput : notnull;
}