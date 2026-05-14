namespace FlexableCsvParser.StateMachine;

internal class EndOfFieldState : StartOfFieldState<EndOfFieldState>, IStateMapProvider
{
    public static StateMap StateMap => StartOfFieldState.StateMap;

    public override ParserState Id => ParserState.EndOfField;
}