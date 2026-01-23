using System;
using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal abstract class BaseState<T> : IState where T : BaseState<T>, IState, new()
{
    public abstract ParserState Id { get; }

    public bool TryTransition(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (StateLookup.TryGetState(token, out nextState))
            return true;

        return TryGetDefault(out nextState);
    }

    public abstract bool TryGetDefault([NotNullWhen(true)] out IState? defaultState);

    public IStateLookup StateLookup
    {
        get => field ?? throw new InvalidOperationException("StartOfFieldState not initialized");
        private set;
    }

    private void Initialize()
    {
        var stateLookupBuilder = new StateLookupBuilder();
        AddStates(stateLookupBuilder);
        StateLookup = stateLookupBuilder.Build();
    }

    protected abstract void AddStates(IStateLookupCollection lookupCollection);

    public static T Instance
    {
        get
        {
            if (field is not null)
                return field;

            field = new T();

            field.Initialize();

            return field;
        }
    }
}