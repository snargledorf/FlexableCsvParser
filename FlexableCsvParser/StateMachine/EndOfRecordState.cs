namespace FlexableCsvParser.StateMachine;

internal class EndOfRecordState : StartOfFieldState<EndOfRecordState>, IStateMapProvider, IDefaultStateProvider
{
    public static StateMap StateMap => StartOfFieldState.StateMap;

    public static BaseState? DefaultState => null;

    public override ParserState Id => ParserState.EndOfRecord;
}