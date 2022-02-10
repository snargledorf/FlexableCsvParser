namespace CsvSpanParser.StateMachine
{
    internal record Transition<TState, TInput>(Func<TInput, bool> CheckInput, TState NewState)
        where TState : notnull
        where TInput : notnull;

}