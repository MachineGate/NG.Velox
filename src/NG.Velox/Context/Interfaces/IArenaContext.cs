namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Memory.Core;

    /// <summary>
    /// Defines a context that provides unified access to a managed reference of a <see cref="MemoryArena"/>.
    /// </summary>
    /// <remarks>
    /// This interface is typically implemented by ref structs or state tracking contexts within the lexing and parsing 
    /// pipelines to facilitate high-performance, zero-allocation memory allocations without boxing or escaping to the heap.
    /// </remarks>
    internal interface IArenaContext
    {
        /// <summary>
        /// Gets a managed reference to the underlying <see cref="MemoryArena"/> used for performance-critical allocations.
        /// </summary>
        /// <value>A mutable reference to the arena allocation bounds.</value>
        ref MemoryArena Arena { get; }
    }
}
