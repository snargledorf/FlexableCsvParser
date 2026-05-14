using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class StateMap
{
    private readonly int _offset;
    private readonly int _length;
    
    private readonly BaseState?[] _states;
    
    public StateMap(IReadOnlyDictionary<CsvTokens, BaseState> states)
    {
        _offset = states.Min(s => (int)s.Key);
        int max = states.Max(s => (int)s.Key);

        _length = max + 1 - _offset;
        _states = new BaseState?[_length];

        foreach (KeyValuePair<CsvTokens, BaseState> state in states)
            _states[state.Key - _offset] = state.Value;
    }

    public bool TryGetState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? state)
    {
        int tokenIndex = token - _offset;
        
        if (tokenIndex > _length || tokenIndex < 0)
        {
            state = null;
            return false;
        }

        state = _states[tokenIndex];
        return state is not null;
    }
}