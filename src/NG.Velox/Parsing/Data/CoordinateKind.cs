namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Specifies the targeted geometric translation axis for a coordinate assignment word in G-code.
    /// </summary>
    /// <remarks>
    /// Explicitly backed by a <see cref="byte"/> to keep the metadata footprint minimal and 
    /// ensure efficient memory packing inside dense parallel parsing buffers or AST node structures.
    /// </remarks>
    internal enum CoordinateKind : byte
    {
        /// <summary>
        /// Represents a displacement, position, or scale assignment along the primary horizontal linear axis (X-word).
        /// </summary>
        X,

        /// <summary>
        /// Represents a displacement, position, or scale assignment along the secondary vertical linear axis (Y-word).
        /// </summary>
        Y,

        /// <summary>
        /// Represents a displacement, position, or depth assignment along the tertiary tool-spindle linear axis (Z-word).
        /// </summary>
        Z
    }
}
