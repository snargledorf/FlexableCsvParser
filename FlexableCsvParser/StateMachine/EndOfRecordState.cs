namespace FlexableCsvParser.StateMachine;

internal class EndOfRecordState : StartOfFieldState<EndOfRecordState>, IStateMapProvider
{
    public static StateMap StateMap => StartOfFieldState.StateMap;

    public override ParserState Id => ParserState.EndOfRecord;
}