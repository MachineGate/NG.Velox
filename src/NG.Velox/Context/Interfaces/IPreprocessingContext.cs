namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Defines the execution context interface for the text preprocessing phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to the immutable source input data alongside the property for publishing 
    /// the preprocessing results. Enforces zero-allocation metrics and enables aggressive JIT inlining.
    /// </remarks>
    internal interface IPreprocessingContext<TInput> : IArenaContext where TInput : notnull
    {
        /// <summary>
        /// Gets the source input data stream or buffer by value.
        /// </summary>
        /// <value>
        /// The <typeparamref name="TInput"/> data structure containing raw text stream segments or lines.
        /// </value>
        TInput Input { get; }

        /// <summary>
        /// Sets the preprocessing result block containing character pointers and layout maps.
        /// </summary>
        /// <value>
        /// The <see cref="PreprocessingResult"/> structural container 
        /// used to store cleaned character streams, character maps, and line lengths.
        /// </value>
        PreprocessingResult PreprocessingResult { set; }
    }
}
