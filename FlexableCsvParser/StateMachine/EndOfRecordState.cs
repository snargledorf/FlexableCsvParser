namespace FlexableCsvParser.StateMachine;

internal class EndOfRecordState : StartOfFieldState<EndOfRecordState>
{
    public override ParserState Id => ParserState.EndOfRecord;
}