namespace NG.Velox.Lexing.Data
{
    /// <summary>
    /// Specifies the syntactic classification category of a lexical token extracted from G-code text.
    /// </summary>
    /// <remarks>
    /// Explicitly backed by a <see cref="byte"/> to minimize the data footprint of the <see cref="Token"/> structure 
    /// and optimize memory layout within sequential array buffers.
    /// </remarks>
    internal enum TokenKind : byte
    {
        /// <summary>
        /// Represents a G-code keyword prefix or command/parameter address letter (e.g., G, M, X, Y, F, S).
        /// </summary>
        Address,

        /// <summary>
        /// Represents a numeric literal value associated with an address, supporting integer, decimal, and scientific notation formats.
        /// </summary>
        Number,

        /// <summary>
        /// Represents an unrecognized, invalid, or malformed character sequence that does not match standard G-code lexical grammar constraints.
        /// </summary>
        Unknown
    }
}
