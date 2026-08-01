namespace NG.Velox.Interpretation.Interfaces
{
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Context.Interfaces;

    /// <summary>
    /// Defines the core contract for execution engines responsible for translating parsed G-code blocks into internal virtual machine states.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface evaluate preprocessed metadata alongside raw syntactic tokens 
    /// to compute deterministic physical positions, feedrates, active motion modes, and manufacturing plane orientations.
    /// </remarks>
    internal interface IVeloxInterpreter
    {
        /// <summary>
        /// Executes the primary lexical interpretation pass over tokenized input blocks 
        /// managed within the provided execution context to populate internal and hardware-ready state containers.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding syntax tokens and interpretation state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where generated execution frames and modal group configurations are recorded.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record modal group conflicts or interpretation anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics, 
        /// preserves data locality, and eliminates value-type copying overhead inside hot interpretation paths.
        /// </remarks>
        void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IInterpretationContext, allows ref struct;
    }
}
