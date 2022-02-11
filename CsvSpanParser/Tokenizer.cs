using System.Runtime.CompilerServices;
using System.Text;

namespace CsvSpanParser
{
	public class Tokenizer : ITokenizer
	{
		protected TextReader Reader { get; }
		protected TokenizerConfig Config { get; }
	
		private readonly Memory<char> readBuffer = new char[4096];
		int readBufferIndex;
		int readBufferLength;

		public Tokenizer(TextReader reader, TokenizerConfig config)
		{
			Reader = reader;
			Config = config;

			var fieldDelimiterChar = config.FieldDelimiter[0];
			if (fieldDelimiterChar != ',' || config.FieldDelimiter.Length > 1)
				throw new ArgumentException("Field delimiter must be ,");

			if (config.RecordDelimiter.Length > 2 || config.RecordDelimiter.Length == 0)
				throw new ArgumentException("Record delimiter may only be 1 or 2 characters");

			var recordDelimiterFirstChar = config.RecordDelimiter[0];

			// TODO Figure out how to implement this in a fast way so we don't have this limitation
			if (recordDelimiterFirstChar != '\r' && recordDelimiterFirstChar != '\n')
				throw new ArgumentException("Record delimiter must be \r\n, \r, or \n");

			if (config.RecordDelimiter.Length == 2)
			{
				var recordDelimiterSecondChar = config.RecordDelimiter[1];

				// TODO Figure out how to implement this in a fast way so we don't have this limitation
				if (recordDelimiterSecondChar != '\n')
					throw new ArgumentException("Record delimiter must be \r\n, \r, or \n");
			}

			if ((config.QuoteDelimiter?.Length ?? 0) > 1)
				throw new ArgumentException("Quote must be 1 character or empty");

			var quoteChar = config.QuoteDelimiter?[0];

			if (quoteChar.HasValue && quoteChar.Value != '"')
				throw new ArgumentException("Quote must be empty or \"");

			if ((config.EscapeDelimiter?.Length ?? 0) > 1)
				throw new ArgumentException("Quote must be 1 character or empty");

			var escapeChar = config.EscapeDelimiter?[0];

			if (escapeChar.HasValue && escapeChar.Value != '"' && escapeChar.Value != '\\')
				throw new ArgumentException("Escape must be empty, \" or \\");
		}

		public virtual async Task<Token> ReadTokenAsync(CancellationToken cancellationToken = default)
		{
			if (readBufferIndex >= readBufferLength)
			{
				readBufferLength = await Reader.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false);

				readBufferIndex = 0;

				if (readBufferLength == 0)
					return Token.EndOfReader;
			}

			return await Task.Run(ReadToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual Token ReadToken()
		{
			const int StartOfEndOfRecord = TokenState.StartOfDelimiterStates;
			int state = TokenState.Start;
			int workingBufferIndex = 0;

			ReadOnlySpan<char> workingBuffer = ReadOnlySpan<char>.Empty;
			StringBuilder? valueBuilder = null;

			while (CheckReadBuffer())
			{
				workingBuffer = readBuffer.Span[readBufferIndex..readBufferLength];
				workingBufferIndex = 0;
				do
				{
					char c = workingBuffer[workingBufferIndex];

					switch (state)
					{
						case TokenState.Start:
							state = c switch
							{
								',' => TokenState.EndOfFieldDelimiter,
								'\r' => StartOfEndOfRecord,
								'\n' => TokenState.EndOfEndOfRecord,
								'"' => TokenState.EndOfQuote,
								'\\' => TokenState.EndOfEscape,
								_ => char.IsWhiteSpace(c) ? TokenState.WhiteSpace : TokenState.Text
							};
							break;

						case TokenState.WhiteSpace:
							if (!char.IsWhiteSpace(c) || c == '\r')
								return CreateToken(TokenType.WhiteSpace, ref valueBuilder, workingBuffer[..workingBufferIndex]);
							break;

						case TokenState.Text:
							if (char.IsWhiteSpace(c) || c == ',' || c == '"' || c == '\\')
								return CreateToken(TokenType.Text, ref valueBuilder, workingBuffer[..workingBufferIndex]);
							break;

						case StartOfEndOfRecord:
							if (c == '\n')
								state = TokenState.EndOfEndOfRecord;
							else
							{
								state = TokenState.WhiteSpace;
								goto case TokenState.WhiteSpace;
							}
							break;

						case TokenState.EndOfFieldDelimiter:
							return Token.FieldDelimiter;

						case TokenState.EndOfEndOfRecord:
							return Token.EndOfRecord;

						case TokenState.EndOfQuote:
							return Token.Quote;

						case TokenState.EndOfEscape:
							return Token.Escape;
					}

					readBufferIndex++;
					workingBufferIndex++;
				} while (workingBufferIndex < workingBuffer.Length);

				if (valueBuilder is null)
					valueBuilder = new StringBuilder(workingBuffer.Length + 10).Append(workingBuffer);
				else
					valueBuilder.Append(workingBuffer);
			}

			return state switch
			{
				TokenState.EndOfFieldDelimiter => Token.FieldDelimiter,
				TokenState.EndOfEndOfRecord => Token.EndOfRecord,
				TokenState.EndOfQuote => Token.Quote,
				TokenState.EndOfEscape => Token.Escape,
				TokenState.Start => Token.EndOfReader,
				_ => CreateToken(state == TokenState.WhiteSpace ? TokenType.WhiteSpace : TokenType.Text, ref valueBuilder, workingBuffer[..workingBufferIndex])
			};

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Token CreateToken(in TokenType type, ref StringBuilder? valueBuilder, in ReadOnlySpan<char> buffer)
			{
				return new Token(type, valueBuilder?.Append(buffer).ToString() ?? new string(buffer));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckReadBuffer()
		{
			if (readBufferIndex >= readBufferLength)
			{
				readBufferLength = Reader.Read(readBuffer.Span);
				readBufferIndex = 0;
			}

			return readBufferLength != 0;
		}
    }

    struct TokenState
	{
		public const int Start = 0;
		public const int EndOfFieldDelimiter = Start + 1;
		public const int EndOfEndOfRecord = EndOfFieldDelimiter + 1;
		public const int EndOfQuote = EndOfEndOfRecord + 1;
		public const int EndOfEscape = EndOfQuote + 1;
		public const int WhiteSpace = EndOfEscape + 1;
		public const int EndOfWhiteSpace = WhiteSpace + 1;
		public const int Text = EndOfWhiteSpace + 1;
		public const int EndOfText = Text + 1;
		public const int StartOfDelimiterStates = EndOfText + 1;
	}
}