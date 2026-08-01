using System.Runtime.CompilerServices;

namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Acts as a high-capacity, zero-allocation immutable container for storing Abstract Syntax Tree (AST) node sequences generated during G-code syntactic parsing.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps data tightly aligned inside a contiguous memory array 
    /// of 16-byte packed <see cref="Node"/> elements to ensure maximal CPU cache locality, rapid linear traversal, and optimal parsing throughput.
    /// <para/>
    /// Because it provides direct raw pointer access, downstream layers can iterate over syntax structures with zero bounds-checking overhead, 
    /// completely bypassing the managed heap. Unlike classic pooled collections, this structure does not own the underlying memory and does not 
    /// require explicit finalization via <c>Dispose</c>. Its entire lifecycle is bound to the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct ParsingResult
    {
        private readonly Node* _nodes;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="ParsingResult"/> structure using a direct unmanaged pointer to the generated AST block.
        /// </summary>
        /// <param name="nodes">A direct unmanaged pointer to the start of the contiguous syntax tree node buffer in memory.</param>
        /// <param name="length">The total number of fully evaluated and valid AST nodes contained within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParsingResult(Node* nodes, int length)
        {
            _nodes = nodes;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded syntax tree node collection.
        /// </summary>
        /// <value>A raw pointer of type <see cref="Node"/>* pointing directly to the first layout slot inside the arena allocation block.</value>
        public readonly Node* NodesPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _nodes;
        }

        /// <summary>
        /// Gets the total number of fully evaluated and valid AST nodes recorded within this syntax sequence.
        /// </summary>
        /// <value>An integer tracking the exact element count available for linear iteration.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the recorded syntax tree node collection.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<Node> Nodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_nodes, _length);
        }

        /// <summary>
        /// Retrieves an AST node at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the syntax node within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="Node"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        public readonly ref readonly Node this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
                return ref _nodes[index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the ParsingResult.");
    }
}
