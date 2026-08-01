namespace NG.Velox.Lexing.Data
{
    /// <summary>
    /// Represents a lightweight, data-packed syntax token identified within a G-code source line.
    /// </summary>
    /// <remarks>
    /// This structure acts as a transient flyweight, storing only structural metadata and offsets 
    /// instead of allocating string copies. This design completely eliminates heap overhead during 
    /// heavy tokenization loops.
    /// </remarks>
    internal readonly struct Token
    {
        /// <summary>
        /// Gets the zero-based starting character position offset relative to the original source text origin.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// Gets the total character count spanned by this individual syntactic token.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Gets the syntactic classification type of this token.
        /// </summary>
        public readonly TokenKind Kind;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> structure with designated token classification boundaries.
        /// </summary>
        /// <param name="kind">The architectural classification type of the token.</param>
        /// <param name="start">The absolute zero-based starting offset inside the scanned text block.</param>
        /// <param name="length">The character slice span length.</param>
        public Token(TokenKind kind, int start, int length)
        {
            Kind = kind;
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Extracts the raw string slice slice backing this token directly out of the source text buffer without allocations.
        /// </summary>
        /// <param name="fullText">The original input continuous read-only span character buffer that was scanned.</param>
        /// <returns>A lightweight <see cref="ReadOnlySpan{Char}"/> mapping directly to the token characters boundaries.</returns>
        public ReadOnlySpan<char> GetValue(ReadOnlySpan<char> fullText)
            => fullText.Slice(Start, Length);
    }
}
