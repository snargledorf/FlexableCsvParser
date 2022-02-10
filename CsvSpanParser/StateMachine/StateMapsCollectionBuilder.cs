namespace CsvSpanParser.StateMachine
{
    internal class StateMapsCollectionBuilder<TState, TInput> : IStateMapsCollectionBuilder<TState, TInput> where TState : notnull where TInput : notnull
    {
        public IStateMapsCollection<TState, TInput> Build()
        {
            throw new NotImplementedException();
        }

        public IStateMapBuilder<TState, TInput> From(TState state)
        {
            return new StateMapBuilder<TState, TInput>(state);
        }
    }
}