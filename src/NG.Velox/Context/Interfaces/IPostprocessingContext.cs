namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Postprocessing.Data;

    /// <summary>
    /// Defines the execution context interface for the postprocessing and validation phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to the final physical moves and micro-points alongside the property 
    /// for publishing the standalone binary payload or simulation metrics.
    /// </remarks>
    internal interface IPostprocessingContext<TOutput> : IArenaContext where TOutput : unmanaged
    {
        /// <summary>
        /// Gets the hardware-interpreted machine data state.
        /// </summary>
        /// <value>
        /// The <see cref="InterpretationResult"/> structure containing original target vector definitions.
        /// </value>
        InterpretationResult InterpretationResult { get; }

        /// <summary>
        /// Gets the high-frequency micro-point sequences.
        /// </summary>
        /// <value>
        /// The <see cref="InterpolationResult"/> structure containing final velocity points or timer ticks.
        /// </value>
        InterpolationResult InterpolationResult { get; }

        /// <summary>
        /// Sets the final unmanaged output layout metadata.
        /// </summary>
        /// <value>
        /// The <see cref="PostprocessingResult{TOutput}"/> structural container used to wrap binary payloads.
        /// </value>
        PostprocessingResult<TOutput> PostprocessingResult { set; }
    }
}
