using Tokensharp;

namespace FlexableCsvParser;

internal record CsvTokens(string Identifier) : TokenType<CsvTokens>(Identifier), ITokenType<CsvTokens>
{
    public static readonly CsvTokens FieldDelimiter = new("field_delimiter");
    public static readonly CsvTokens EndOfRecord = new("end_of_record");
    public static readonly CsvTokens Quote = new("quote");
    public static readonly CsvTokens Escape = new("escape");
    
    public static CsvTokens Create(string lexeme) => new(lexeme);

    public static TokenConfiguration<CsvTokens> Configuration { get; } = new TokenConfigurationBuilder<CsvTokens>(
    [
        new LexemeToTokenType<CsvTokens>(",", FieldDelimiter),
        new LexemeToTokenType<CsvTokens>("\r\n", EndOfRecord),
        new LexemeToTokenType<CsvTokens>("\"", Quote),
        new LexemeToTokenType<CsvTokens>("\"\"", Escape),
    ]) { NumbersAreText = true }.Build();
}