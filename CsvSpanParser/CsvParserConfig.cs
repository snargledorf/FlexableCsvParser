namespace CsvSpanParser
{
    public struct CsvParserConfig
    {
        public static readonly CsvParserConfig Default = new();
        public static readonly CsvParserConfig RFC4180 = Default;

        public CsvParserConfig()
            : this(Delimiters.Default)
        {
        }

        public CsvParserConfig(string field = ",", string endOfRecord = "\r\n", string quote = "\"", string escape = "\"\"")
            : this(new Delimiters(field, endOfRecord, quote, escape))
        {
        }

        public CsvParserConfig(Delimiters delimiters)
        {
            Delimiters = delimiters;
        }

        public Delimiters Delimiters { get; }
    }
}