namespace NG.Velox.Interpolation.Interfaces
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;

    /// <summary>
    /// Defines the core contract for high-performance kinematic trajectory interpolators.
    /// </summary>
    /// <remarks>
    /// Implementation of this interface are responsible for generating precise motion coordinates 
    /// (time-slices, steps, and velocity changes) by blending pre-calculated block states with 
    /// actual physical system constraints.
    /// </remarks>
    internal interface IVeloxInterpolator
    {
        /// <summary>
        /// Executes the trajectory interpolation algorithm over a pre-planned block sequence 
        /// managed within the provided execution context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding kinematic profiles and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where generated trajectory points, segments, and coordinate frame mappings are recorded.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record interpolation anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics, 
        /// preserves data locality, and eliminates value-type copying overhead inside hot trajectory interpolation loops.
        /// </remarks>
        void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IInterpolationContext, allows ref struct;
    }
}
