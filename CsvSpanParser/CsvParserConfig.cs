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

        public CsvParserConfig(
            string field = ",",
            string endOfRecord = "\r\n",
            string quote = "\"",
            string escape = "\"\"",
            int recordLength = 0,
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException)
            : this(
                new Delimiters(field, endOfRecord, quote, escape),
                recordLength,
                incompleteRecordHandling)
        {
        }

        public CsvParserConfig(
            Delimiters delimiters,
            int recordLength = 0,
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException)
        {
            Delimiters = delimiters;
            RecordLength = recordLength;
            IncompleteRecordHandling = incompleteRecordHandling;
        }

        public Delimiters Delimiters { get; }

        public int RecordLength { get; set; }
        public IncompleteRecordHandling IncompleteRecordHandling { get; set; }
    }
}