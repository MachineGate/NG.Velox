namespace NG.Velox.Lexing.Interfaces
{
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Context.Interfaces;

    /// <summary>
    /// Defines the core contract for performance-critical lexical analyzers responsible for tokenizing G-code source text.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface scan raw character buffers or preprocessed lines to isolate, 
    /// classify, and record lightweight syntax tokens. The operation runs in a strict zero-allocation loop 
    /// to ensure maximum throughput before parsing and interpretation phases.
    /// </remarks>
    internal interface IVeloxLexer
    {
        /// <summary>
        /// Scans the preprocessed input blocks and decomposes them into a stream of categorized lexical tokens 
        /// managed within the provided execution context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding preprocessed blocks and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where newly generated syntax tokens and classification states are recorded.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record early lexical anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics, 
        /// preserves data locality, and eliminates value-type copying overhead inside hot lexical scanning paths.
        /// </remarks>
        void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, ILexingContext, allows ref struct;
    }
}
