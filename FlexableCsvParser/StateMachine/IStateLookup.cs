using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal interface IStateLookup
{
    public bool TryGetState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? state);
}

internal class StateLookupBuilder : IStateLookupCollection
{
    private readonly Dictionary<TokenType<CsvTokens>, BaseState> _states = new();

    public IStateLookupCollection Add(CsvTokens token, BaseState state)
    {
        _states.Add(token, state);
        return this;
    }
    
    public IStateLookup Build()
    {
        return new StateLookup(_states.ToFrozenDictionary());
    }
}

internal class StateLookup(FrozenDictionary<TokenType<CsvTokens>, BaseState> states) : IStateLookup
{
    public bool TryGetState(TokenType<CsvTokens> token, [NotNullWhen(true)] out BaseState? state)
    {
        return states.TryGetValue(token, out state);
    }
}