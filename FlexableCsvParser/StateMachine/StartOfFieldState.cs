using System.Diagnostics.CodeAnalysis;
using Tokensharp;

namespace FlexableCsvParser.StateMachine;

internal class StartOfFieldState<T> : BaseState<T> where T : StartOfFieldState<T>, IState, new()
{
    public override ParserState Id => ParserState.StartOfField;

    protected override bool TryGetNextState(TokenType<CsvTokens> token, [NotNullWhen(true)] out IState? nextState)
    {
        if (token == CsvTokens.Text || token == CsvTokens.Number)
        {
            nextState = UnquotedFieldTextState.Instance;
            return true;
        }

        if (token == CsvTokens.Quote)
        {
            nextState = QuotedFieldOpenQuoteState.Instance;
            return true;
        }

        if (token == CsvTokens.WhiteSpace)
        {
            nextState = LeadingWhiteSpaceState.Instance;
            return true;
        }
        
        if (token == CsvTokens.FieldDelimiter)
        {
            nextState = EndOfFieldState.Instance;
            return true;
        }
        
        if (token == CsvTokens.EndOfRecord)
        {
            nextState = EndOfRecordState.Instance;
            return true;
        }

        if (token == CsvTokens.Escape)
        {
            nextState = LeadingEscapeState.Instance;
            return true;
        }

        return TryGetDefault(out nextState);
    }

    public override bool TryGetDefault([NotNullWhen(true)] out IState? defaultState)
    {
        defaultState = null;
        return false;
    }
}

internal class StartOfFieldState : StartOfFieldState<StartOfFieldState>
{
}