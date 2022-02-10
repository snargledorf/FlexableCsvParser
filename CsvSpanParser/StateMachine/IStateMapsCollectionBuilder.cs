namespace CsvSpanParser.StateMachine
{
    public interface IStateMapsCollectionBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        IStateMapsCollection<TState, TInput> Build();
        
        IStateMapBuilder<TState, TInput> From(TState state);
    }
}