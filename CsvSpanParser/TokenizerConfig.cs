namespace CsvSpanParser
{
    public sealed class TokenizerConfig
    {
        public string FieldDelimiter { get; set; } = ",";
        public string RecordDelimiter { get; set; } = "\r\n";
        public string QuoteDelimiter { get; set; } = "\"";
        public string EscapeDelimiter { get; set; } = "\"";
    }
}