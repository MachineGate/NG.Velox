namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Specifies the primary classification category of an Abstract Syntax Tree (AST) node.
    /// </summary>
    /// <remarks>
    /// Explicitly backed by a <see cref="byte"/> to serve as a compact data discriminator 
    /// within the explicit 16-byte union layout of the <see cref="Node"/> structure.
    /// </remarks>
    internal enum NodeKind : byte
    {
        /// <summary>
        /// Indicates the node represents an auxiliary modifier or geometric offset parameter (e.g., I, J, K, R, F, S).
        /// Maps directly to the <see cref="ParameterKind"/> field in overlapping memory.
        /// </summary>
        Parameter,

        /// <summary>
        /// Indicates the node represents a preparatory or miscellaneous machine command (G or M codes).
        /// Maps directly to the <see cref="CommandKind"/> field in overlapping memory.
        /// </summary>
        Command,

        /// <summary>
        /// Indicates the node represents a target displacement axis configuration (X, Y, or Z).
        /// Maps directly to the <see cref="CoordinateKind"/> field in overlapping memory.
        /// </summary>
        Coordinate
    }
}
