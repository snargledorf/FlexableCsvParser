namespace FlexableCsvParser
{
    public sealed class CsvParserConfig
    {
        public static readonly CsvParserConfig RFC4180 = new CsvParserConfig(Delimiters.RFC4180);
        public static readonly CsvParserConfig Default = RFC4180;

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
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException,
            WhiteSpaceTrimming whiteSpaceTrimming = WhiteSpaceTrimming.None)
            : this(
                new Delimiters(field, endOfRecord, quote, escape),
                recordLength,
                incompleteRecordHandling,
                whiteSpaceTrimming)
        {
        }

        public CsvParserConfig(
            Delimiters delimiters,
            int recordLength = 0,
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException,
            WhiteSpaceTrimming whiteSpaceTrimming = WhiteSpaceTrimming.None)
        {
            Delimiters = delimiters;
            RecordLength = recordLength;
            IncompleteRecordHandling = incompleteRecordHandling;
            WhiteSpaceTrimming = whiteSpaceTrimming;
        }

        public Delimiters Delimiters { get; }

        public int RecordLength { get; set; }
        public IncompleteRecordHandling IncompleteRecordHandling { get; set; }

        public WhiteSpaceTrimming WhiteSpaceTrimming { get; set; }
    }
}