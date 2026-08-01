namespace NG.Velox.Pipeline.Interfaces
{
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Memory.Core;

    /// <summary>
    /// Defines the definitive v2.0.0 core contract for performance-critical manufacturing data compilation pipelines.
    /// </summary>
    /// <typeparam name="TInput">The type of input data (typically <see cref="ReadOnlyMemory{T}"/> of char).</typeparam>
    /// <typeparam name="TOutput">The type of output data (e.g., byte for hardware, SimulationFrame for visualization).</typeparam>
    /// <remarks>
    /// Implementations should be thread-safe and reusable across multiple calls.
    /// </remarks>
    public interface IVeloxPipeline<TInput, TOutput>
        where TInput : notnull
        where TOutput : unmanaged
    {
        /// <summary>
        /// Executes the full manufacturing compilation конвейер, returning a strictly unmanaged slice of results.
        /// </summary>
        /// <param name="input">The unstructured data payload entering the compilation environment.</param>
        /// <param name="arena">A reference to the active execution <see cref="MemoryArena"/> where all internal and output allocations are pinned.</param>
        /// <param name="diagnosticBag">A reference to the localized compilation logger used to watch execution safety states.</param>
        /// <returns>An immutable <see cref="PipelineResult{TOutput}"/> snapshot pointing directly to the compiled assets inside the arena.</returns>
        PipelineResult<TOutput> Process(TInput input, ref MemoryArena arena, ref DiagnosticBag diagnosticBag);
    }
}