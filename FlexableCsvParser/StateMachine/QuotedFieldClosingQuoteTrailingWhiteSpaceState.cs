using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class QuotedFieldClosingQuoteTrailingWhiteSpaceState : BaseState<QuotedFieldClosingQuoteTrailingWhiteSpaceState>
{
    public override ParserState Id => ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.EndOfRecord)
        {
            nextState = EndOfRecordState.Instance;
            return true;
        }
        
        if (token == CsvTokens.Text || token == CsvTokens.Quote || token == CsvTokens.Escape)
        {
            nextState = UnexpectedTokenState.Instance;
            return true;
        }
        
        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = EndOfFieldState.Instance;
        return true;
    }
}