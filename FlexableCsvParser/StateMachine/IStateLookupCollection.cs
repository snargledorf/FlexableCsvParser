namespace FlexableCsvParser.StateMachine;

internal interface IStateLookupCollection
{
    IStateLookupCollection Add(CsvTokens token, IState state);
}