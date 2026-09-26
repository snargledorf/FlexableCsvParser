using System.Collections.Generic;
using Tokensharp;

namespace FlexableCsvParser;

internal sealed class MatrixBuilder(int columnCount)
{
    private readonly Dictionary<ParserState, ParserState[]> _transitionMatrix = new();
    
    public MatrixBuilder SetDefault(ParserState state, ParserState defaultState)
    {
        ParserState[] stateTransitions = GetParserStateTransitions(state);
        for (var column = 0; column < columnCount; column++) 
            stateTransitions[column] = defaultState;
        
        return this;
    }

    public MatrixBuilder Set(ParserState state, CsvTokens token, ParserState value)
    {
        ParserState[] stateTransitions = GetParserStateTransitions(state);
        stateTransitions[(int)token] = value;
        return this;
    }

    public int[,] Build()
    {
        var matrix = new int[_transitionMatrix.Count, columnCount];
        
        foreach ((var state, ParserState[] stateTransitions) in _transitionMatrix)
        {
            for (int column = 0; column < columnCount; column++)
                matrix[(int)state, column] = (int)stateTransitions[column];
        }

        return matrix;
    }

    private ParserState[] GetParserStateTransitions(ParserState state) => _transitionMatrix.GetOrAdd(state, _ => new ParserState[columnCount]);
}