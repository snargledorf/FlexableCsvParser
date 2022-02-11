namespace CsvSpanParser
{
    public struct TokenizerConfig
    {
        public static readonly TokenizerConfig Default = new();
        public static readonly TokenizerConfig RFC4180 = Default;

        public TokenizerConfig()
            : this(",", "\r\n", "\"", "\"\"")
        {
        }

        public TokenizerConfig(string fieldDelimiter = ",", string endOfRecord = "\r\n", string quote = "\"", string escape = "\"\"")
        {
            FieldDelimiter = fieldDelimiter;
            EndOfRecord = endOfRecord;
            Quote = quote;
            Escape = escape;
        }

        public string FieldDelimiter { get; }
        public string EndOfRecord { get; }
        public string Quote { get; }
        public string Escape { get; }
    }
}