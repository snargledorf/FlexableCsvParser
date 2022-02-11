namespace CsvSpanParser
{
    public struct CsvParserConfig
    {
        public static readonly CsvParserConfig Default = new();
        public static readonly CsvParserConfig RFC4180 = Default;

        public CsvParserConfig()
            : this(",", "\r\n", "\"", "\"\"")
        {
        }

        public CsvParserConfig(string fieldDelimiter = ",", string endOfRecord = "\r\n", string quote = "\"", string escape = "\"\"")
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

        internal TokenizerConfig ToTokenizerConfig()
        {
            return new TokenizerConfig(FieldDelimiter, EndOfRecord, Quote, Escape);
        }
    }
}