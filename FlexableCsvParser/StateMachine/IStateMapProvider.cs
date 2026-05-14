namespace FlexableCsvParser.StateMachine;

internal interface IStateMapProvider
{
    static abstract StateMap StateMap { get; }
}