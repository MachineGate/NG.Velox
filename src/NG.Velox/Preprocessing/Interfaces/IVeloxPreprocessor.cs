namespace NG.Velox.Preprocessing.Interfaces
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;

    /// <summary>
    /// Defines the core contract for performance-critical preprocessors responsible for normalizing raw G-code input formats.
    /// </summary>
    /// <typeparam name="TInput">The target source data format type entering the pipeline loop.</typeparam>
    /// <remarks>
    /// Implementations of this interface filter incoming characters, strip comments, handle layout spacing, 
    /// and build initial index mappings completely allocation-free before initiating lexical scanning.
    /// </remarks>
    internal interface IVeloxPreprocessor<TInput>
        where TInput : notnull
    {
        /// <summary>
        /// Executes the initial text scanning, filtering, and source mapping pass over the unstructured raw input 
        /// encapsulated within the provided compilation context.
        /// </summary>
        /// <typeparam name="TContext">The specific context type holding the input payload and state, constrained to zero-allocation structures.</typeparam>
        /// <param name="context">A reference to the mutable <typeparamref name="TContext"/> where normalized characters, index maps, and current parsing state are managed.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states and record early anomalies.</param>
        /// <remarks>
        /// Enforces strict usage of ref modifiers across all structures combined with the anti-boxing 
        /// <see langword="allows ref struct"/> constraint. This guarantees zero-allocation execution characteristics 
        /// and eliminates value-type copying overhead inside hot data parsing paths.
        /// </remarks>
        void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPreprocessingContext<TInput>, allows ref struct;
    }
}
