namespace NG.Velox.Planning.Interfaces
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;

    /// <summary>
    /// Defines the core contract for performance-critical look-ahead velocity planners in the CNC pipeline.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface evaluate interpreted virtual machine frames to compute optimal junction 
    /// velocities, apply acceleration constraints, and build smooth execution profiles before path interpolation.
    /// </remarks>
    internal interface IVeloxPlanner
    {
        /// <summary>
        /// Executes the look-ahead kinematic planning pass over interpreted virtual machine blocks 
        /// managed within the provided execution context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding kinematic profiles and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where optimized block execution velocities and acceleration profile metadata are recorded.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record kinematic validation anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics, 
        /// preserves data locality, and eliminates value-type copying overhead inside hot kinematic planning paths.
        /// </remarks>
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPlanningContext, allows ref struct;
    }
}
