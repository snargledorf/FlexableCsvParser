namespace FlexableCsvParser.StateMachine;

internal class EndOfFieldState : StartOfFieldState<EndOfFieldState>
{
    public override ParserState Id => ParserState.EndOfField;
}