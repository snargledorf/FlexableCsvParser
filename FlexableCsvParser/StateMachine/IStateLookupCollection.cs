namespace FlexableCsvParser.StateMachine;

internal interface IStateLookupCollection
{
    IStateLookupCollection Add(CsvTokens token, BaseState state);
}