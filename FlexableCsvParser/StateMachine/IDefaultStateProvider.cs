namespace FlexableCsvParser.StateMachine;

internal interface IDefaultStateProvider
{
    static abstract BaseState? DefaultState { get; }
}