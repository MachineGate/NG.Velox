namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Specifies the architectural category of a structural CNC instruction code.
    /// </summary>
    /// <remarks>
    /// Explicitly backed by a <see cref="byte"/> to maintain a compact data footprint and 
    /// ensure memory alignment within performance-critical ast tracking and parsing arrays.
    /// </remarks>
    internal enum CommandKind : byte
    {
        /// <summary>
        /// Represents a preparatory function command (G-code) responsible for configuring geometry, 
        /// interpolation modes, and coordinate systems (e.g., G00, G01, G17).
        /// </summary>
        G,

        /// <summary>
        /// Represents a miscellaneous machine action command (M-code) responsible for controlling hardware 
        /// auxiliary functions, macro states, or block execution signals (e.g., M03, M05, M08).
        /// </summary>
        M
    }
}
