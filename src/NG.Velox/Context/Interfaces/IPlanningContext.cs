namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Planning.Data;

    /// <summary>
    /// Defines the execution context interface for the look-ahead planning phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to interpreted machine profiles alongside the property for publishing 
    /// optimized, kinematic execution paths (such as look-ahead buffers and junction speed caps).
    /// </remarks>
    internal interface IPlanningContext : IArenaContext
    {
        /// <summary>
        /// Gets the intermediate hardware state frames and block coordinates.
        /// </summary>
        /// <value>
        /// The <see cref="InterpretationResult"/> structure containing parsed vector positions and raw physical moves.
        /// </value>
        InterpretationResult InterpretationResult { get; }

        /// <summary>
        /// Sets the planning result container where optimized speed maps and profiles are recorded.
        /// </summary>
        /// <value>
        /// The <see cref="PlanningResult"/> structural container used to store calculated speed ramps and corner limitations.
        /// </value>
        PlanningResult PlanningResult { set; }
    }
}
