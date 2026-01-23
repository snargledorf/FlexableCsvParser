using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal interface IStateLookup
{
    public bool TryGetState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? state);
}

internal class StateLookupBuilder : IStateLookupCollection
{
    private readonly Dictionary<TokenType<CsvTokens>, IState> _states = new();

    public IStateLookupCollection Add(CsvTokens token, IState state)
    {
        _states.Add(token, state);
        return this;
    }
    
    public IStateLookup Build()
    {
        return new StateLookup(_states.ToFrozenDictionary());
    }
}

internal class StateLookup(FrozenDictionary<TokenType<CsvTokens>, IState> states) : IStateLookup
{
    public bool TryGetState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? state)
    {
        return states.TryGetValue(token, out state);
    }
}