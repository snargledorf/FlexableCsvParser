namespace FlexableCsvParser
{
    public sealed class Delimiters
    {
        public static readonly Delimiters RFC4180 = new Delimiters();
        public static readonly Delimiters Default = RFC4180;

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
                && EndOfRecord == "\r\n"
                && Quote == "\""
                && Escape == "\"\"";
        }

        public string Field { get; }
        public string EndOfRecord { get; }
        public string Quote { get; }
        public string Escape { get; }
        public bool IsRFC4180Compliant { get; }
    }
}