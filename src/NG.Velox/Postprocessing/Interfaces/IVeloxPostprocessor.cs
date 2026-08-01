namespace NG.Velox.Postprocessing.Interfaces
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;

    /// <summary>
    /// Defines the core contract for performance-critical postprocessors responsible for translating interpolated toolpaths into final system outputs.
    /// </summary>
    /// <typeparam name="TOutput">The resolved data format type compiled by the postprocessing layer.</typeparam>
    /// <remarks>
    /// Implementations of this interface consume the outputs of both the interpreter and the interpolator stages. 
    /// They format or serialize this data into specific target layouts, such as binary streams for machinery hardware controllers 
    /// or geometric collections for simulation rendering pipelines.
    /// </remarks>
    internal interface IVeloxPostprocessor<TOutput>
        where TOutput : unmanaged
    {
        /// <summary>
        /// Executes the compilation and serialization pass over interpreted states and interpolated trajectory points 
        /// managed within the provided execution context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding target segments and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where finalized, formatted target entries and coordinate frame mappings are recorded.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record early anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics 
        /// and eliminates value-type copying overhead inside high-capacity data serialization loops.
        /// </remarks>
        void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPostprocessingContext<TOutput>, allows ref struct;
    }
}
