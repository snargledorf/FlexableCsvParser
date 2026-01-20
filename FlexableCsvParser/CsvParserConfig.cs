using Tokensharp;
using Tokensharp.StateMachine;

namespace FlexableCsvParser
{
    public sealed class CsvParserConfig
    {
        public static readonly CsvParserConfig RFC4180 = new(Delimiters.RFC4180);
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
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException,
            WhiteSpaceTrimming whiteSpaceTrimming = WhiteSpaceTrimming.None,
            int stringCacheMaxLength = 128)
            : this(
                new Delimiters(field, endOfRecord, quote, escape),
                incompleteRecordHandling,
                whiteSpaceTrimming,
                stringCacheMaxLength)
        {
        }

        public CsvParserConfig(
            Delimiters delimiters,
            IncompleteRecordHandling incompleteRecordHandling = IncompleteRecordHandling.ThrowException,
            WhiteSpaceTrimming whiteSpaceTrimming = WhiteSpaceTrimming.None,
            int stringCacheMaxLength = 128)
        {
            Delimiters = delimiters;
            IncompleteRecordHandling = incompleteRecordHandling;
            WhiteSpaceTrimming = whiteSpaceTrimming;
            StringCacheMaxLength = stringCacheMaxLength;
            
            if (!Delimiters.AreRFC4180Compliant)
            {
                TokenConfiguration = new TokenConfigurationBuilder<CsvTokens>()
                {
                    { Delimiters.Field, CsvTokens.FieldDelimiter },
                    { Delimiters.EndOfRecord, CsvTokens.EndOfRecord },
                    { Delimiters.Quote, CsvTokens.Quote },
                    { Delimiters.Escape, CsvTokens.Escape },
                }.Build();
            }
            else
            {
                TokenConfiguration = CsvTokens.Configuration;
            }
        }

        public int StringCacheMaxLength { get; set; }

        public Delimiters Delimiters { get; }
        
        internal TokenConfiguration<CsvTokens> TokenConfiguration { get; }

        public IncompleteRecordHandling IncompleteRecordHandling { get; set; }

        public WhiteSpaceTrimming WhiteSpaceTrimming { get; set; }
    }
}