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
        }

        public string Field { get; }
        public string EndOfRecord { get; }
        public string Quote { get; }
        public string Escape { get; }
    }
}