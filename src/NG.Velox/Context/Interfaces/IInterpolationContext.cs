namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Planning.Data;

    /// <summary>
    /// Defines the execution context interface for the trajectory interpolation phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to interpreted machine data and pre-planned kinematic profiles alongside 
    /// the property for publishing the generated high-frequency micro-point stream.
    /// </remarks>
    internal interface IInterpolationContext : IArenaContext
    {
        /// <summary>
        /// Gets the immutable geometric data produced during the machine interpretation phase.
        /// </summary>
        /// <value>
        /// The <see cref="InterpretationResult"/> structure containing parsed vector definitions and machine frames.
        /// </value>
        InterpretationResult InterpretationResult { get; }

        /// <summary>
        /// Gets the immutable kinematic execution profiles computed during the look-ahead planning phase.
        /// </summary>
        /// <value>
        /// The <see cref="PlanningResult"/> structure containing optimized block execution velocities.
        /// </value>
        PlanningResult PlanningResult { get; }

        /// <summary>
        /// Sets the interpolation buffer where high-frequency trajectory points are recorded.
        /// </summary>
        /// <value>
        /// The <see cref="InterpolationResult"/> structural container used to store generated micro-points.
        /// </value>
        InterpolationResult InterpolationResult { set; }
    }
}
