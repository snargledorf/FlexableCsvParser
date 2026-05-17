namespace FlexableCsvParser;

internal sealed class MatrixBuilder(int[,] transitionMatrix, int columnCount)
{
    public MatrixBuilder SetDefault(ParserState state, ParserState defaultState)
    {
        for (int column = 0; column < columnCount; column++) 
            transitionMatrix[(int)state, column] = (int)defaultState;
        
        return this;
    }

    public MatrixBuilder Set(ParserState state, CsvTokens token, ParserState value)
    {
        transitionMatrix[(int)state, token] = (int)value;
        return this;
    }
}