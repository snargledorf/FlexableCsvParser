namespace FlexableCsvParser.StateMachine;

internal record struct CsvTokenToState(CsvTokens Token, BaseState State);