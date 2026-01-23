using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal interface IState
{
    ParserState Id { get; }

    bool TryTransition(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState);
    bool TryGetDefault([NotNullWhen(true)] out IState? defaultState);
}