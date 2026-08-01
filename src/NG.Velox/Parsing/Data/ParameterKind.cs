namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Specifies the architectural category of an auxiliary G-code modifier or geometric offset parameter.
    /// </summary>
    /// <remarks>
    /// Explicitly backed by a <see cref="byte"/> to serve as a compact data discriminator 
    /// within the explicit 16-byte union layout of the <see cref="Node"/> structure.
    /// </remarks>
    internal enum ParameterKind : byte
    {
        /// <summary>
        /// Represents the feedrate parameter (F-word), defining the target linear or rotary translation velocity.
        /// </summary>
        F,

        /// <summary>
        /// Represents the spindle speed parameter (S-word), configuring the rotational velocity of the tool spindle.
        /// </summary>
        S,

        /// <summary>
        /// Represents an auxiliary parameter or dwell time modifier (P-word), commonly used in macros or timed delays.
        /// </summary>
        P,

        /// <summary>
        /// Represents the primary axis arc center offset component (I-word), parallel to the X-axis.
        /// </summary>
        I,

        /// <summary>
        /// Represents the secondary axis arc center offset component (J-word), parallel to the Y-axis.
        /// </summary>
        J,

        /// <summary>
        /// Represents the tertiary depth axis arc center offset component (K-word), parallel to the Z-axis.
        /// </summary>
        K,

        /// <summary>
        /// Represents the absolute circle radius modifier (R-word) for radius-designated circular or helical interpolation.
        /// </summary>
        R
    }
}
