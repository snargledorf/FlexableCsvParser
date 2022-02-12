namespace CsvSpanParser
{
    public struct Delimiters
    {
        public static readonly Delimiters Default = new();
        public static readonly Delimiters RFC4180 = Default;

        public Delimiters()
            : this(",", "\r\n", "\"", "\"\"")
        {
        }

        public Delimiters(string fieldDelimiter = ",", string endOfRecord = "\r\n", string quote = "\"", string escape = "\"\"")
        {
            Field = fieldDelimiter;
            EndOfRecord = endOfRecord;
            Quote = quote;
            Escape = escape;

            IsRFC4180Compliant = Field == ","
                && (EndOfRecord == "\r\n" || EndOfRecord == "\r" || EndOfRecord == "\n")
                && (string.IsNullOrEmpty(Quote) || Quote == "\"")
                && (string.IsNullOrEmpty(Escape) || Escape == "\"\"");
        }

        public string Field { get; }
        public string EndOfRecord { get; }
        public string Quote { get; }
        public string Escape { get; }
        public bool IsRFC4180Compliant { get; }
    }
}