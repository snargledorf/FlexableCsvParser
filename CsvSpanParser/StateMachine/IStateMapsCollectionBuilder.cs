namespace CsvSpanParser.StateMachine
{
    public interface IStateMapsCollectionBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {        
        IStateMapBuilder<TState, TInput> From(TState state);
    }
}