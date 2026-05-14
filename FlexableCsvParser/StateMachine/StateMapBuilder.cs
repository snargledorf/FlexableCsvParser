using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FlexableCsvParser.StateMachine;

internal class StateMapBuilder : IEnumerable<CsvTokenToState>
{
    private readonly Dictionary<CsvTokens, BaseState> _states = [];

    public StateMapBuilder Add(CsvTokenToState tokenToState) => Add(tokenToState.Token, tokenToState.State);

    public StateMapBuilder Add(CsvTokens token, BaseState state)
    {
        _states.Add(token, state);
        return this;
    }
    
    public StateMap Build() => new(_states);

    public IEnumerator<CsvTokenToState> GetEnumerator()
    {
        return _states.Select(kvp => new CsvTokenToState(kvp.Key, kvp.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}