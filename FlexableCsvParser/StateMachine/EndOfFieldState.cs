namespace FlexableCsvParser.StateMachine;

internal class EndOfFieldState : StartOfFieldState<EndOfFieldState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap => StartOfFieldState.StateMap;

    public static BaseState? DefaultState => null;

    public override ParserState Id => ParserState.EndOfField;
}