namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Lexing.Data;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Defines the execution context interface for the primary lexical analysis phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to the immutable preprocessing results alongside the property for publishing 
    /// the generated tokens. Enforces zero-allocation metrics and eliminates reference-tracking overhead.
    /// </remarks>
    internal interface ILexingContext : IArenaContext
    {
        /// <summary>
        /// Gets the immutable results produced during the initial text preprocessing phase.
        /// </summary>
        /// <value>
        /// The <see cref="PreprocessingResult"/> structure containing raw character stream pointers 
        /// and boundary tracking maps.
        /// </value>
        PreprocessingResult PreprocessingResult { get; }

        /// <summary>
        /// Sets the lexical result block containing generated token structures.
        /// </summary>
        /// <value>
        /// The <see cref="LexingResult"/> structural container used to store unmanaged token lists.
        /// </value>
        LexingResult LexingResult { set; }
    }
}
