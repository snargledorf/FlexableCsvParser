# FlexableCsvParser

A high-performance, flexible, and zero-allocation oriented CSV parser for modern .NET.

## Key Features

* 🚀 **High Performance:** Designed with performance in mind, utilizing `Span<char>`, `ReadOnlySpan<char>`, and `StringPool` to minimize allocations and maximize throughput.
* 🛠️ **Extremely Flexible:** Support for custom delimiters of any length for fields, records, quotes, and escape sequences.
* ✅ **RFC 4180 Compliant:** Full support for RFC 4180 by default.
* 🧹 **Configurable Trimming:** Built-in support for leading and trailing white space trimming.
* 🛡️ **Robust Error Handling:** Configurable behavior for handling incomplete records (Throw, Fill, or Truncate).
* 🧩 **Async Support:** Provides asynchronous APIs for non-blocking I/O.
* 🧶 **String Interning:** Integrated `StringPool` to reduce memory footprint by interning repeated field values.

## Installation

Install via NuGet:

```bash
dotnet add package FlexableCsvParser
```

## Quick Start

The simplest way to get started is using the default RFC 4180 configuration.

```csharp
using System.IO;
using FlexableCsvParser;

const string csv = "123, \"456 ,\"\"789\"\"\" ,ABC";
int expectedFieldCount = 3;

// Initialize the parser with a TextReader and expected field count
var parser = new CsvParser(new StringReader(csv), expectedFieldCount);

string[] record = new string[expectedFieldCount];

// Read records into a span or array
while (parser.ReadRecord(record) > 0)
{
    Console.WriteLine($"Field 1: {record[0]}");
    Console.WriteLine($"Field 2: {record[1]}");
    Console.WriteLine($"Field 3: {record[2]}");
}
```

## Advanced Usage

### Custom Delimiters

FlexableCsvParser excels at handling non-standard CSV formats with multi-character delimiters.

```csharp
const string csv = "123<Field> <Quote>456<Field>789<Quote><Field>ABC<Record>";

var config = new CsvParserConfig(
    field: "<Field>",
    endOfRecord: "<Record>",
    quote: "<Quote>",
    escape: "<Quote><Quote>"
);

var parser = new CsvParser(new StringReader(csv), 3, config);
```

### Configuration Options

The `CsvParserConfig` class provides several options to tune the parser's behavior:


| Option                     | Description                                                  | Default              |
| :------------------------- | :----------------------------------------------------------- | :------------------- |
| `Delimiters`               | Defines the field, record, quote, and escape sequences.      | `Delimiters.Rfc4180` |
| `IncompleteRecordHandling` | How to handle records with fewer fields than expected.       | `ThrowException`     |
| `WhiteSpaceTrimming`       | Whether to trim leading or trailing white space.             | `None`               |
| `StringCacheMaxLength`     | Maximum length of strings to be cached in the `StringPool`. | `128`                |

### Incomplete Record Handling

You can control how the parser reacts when it encounters a record that doesn't match the expected field count.

```csharp
var config = new CsvParserConfig
{
    IncompleteRecordHandling = IncompleteRecordHandling.FillInWithNull
};
```

Available modes:

* `ThrowException`: Throws an `InvalidDataException`.
* `FillInWithEmpty`: Fills missing fields with `string.Empty`.
* `FillInWithNull`: Fills missing fields with `null`.
* `TruncateRecord`: Returns only the fields that were successfully read.

### White Space Trimming

Trimming can be applied to unquoted and quoted fields.

```csharp
var config = new CsvParserConfig
{
    WhiteSpaceTrimming = WhiteSpaceTrimming.Both
};
```

## Async Support

For high-throughput applications, use the async API:

```csharp
string[] record = new string[3];
while (await parser.ReadRecordAsync(record) > 0)
{
    // Process record
}
```

## Performance Note

FlexableCsvParser is built on top of `Tokensharp` and utilizes modern .NET features to ensure minimal overhead. By using `Span<string>` for record output and an internal `StringPool`, it significantly reduces the GC pressure typically associated with CSV parsing.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
