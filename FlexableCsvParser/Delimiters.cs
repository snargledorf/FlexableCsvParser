using System;

namespace FlexableCsvParser
{
    public sealed record Delimiters
    {
        public static readonly Delimiters Rfc4180 = new();
        public static readonly Delimiters Default = Rfc4180;

        public Delimiters(string fieldDelimiter = ",", string endOfRecord = "\r\n", string quote = "\"", string escape = "\"\"")
        {
            Field = fieldDelimiter;
            EndOfRecord = endOfRecord;
            Quote = quote;
            Escape = escape;

            AreRfc4180Compliant = Field == ","
                && EndOfRecord == "\r\n"
                && Quote == "\""
                && Escape == "\"\"";

            if (string.IsNullOrEmpty(fieldDelimiter))
                throw new ArgumentException("Field delimiter is required");

            if (string.IsNullOrEmpty(endOfRecord))
                throw new ArgumentException("End of record is required");

            if (string.IsNullOrEmpty(quote) && !string.IsNullOrEmpty(escape))
                throw new ArgumentException("Escape must be blank if quote is blank");
        }

        public string Field { get; }
        public string EndOfRecord { get; }
        public string Quote { get; }
        public string Escape { get; }
        public bool AreRfc4180Compliant { get; }
    }
}