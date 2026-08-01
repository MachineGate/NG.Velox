using System.Runtime.CompilerServices;

namespace NG.Velox.Postprocessing.Data
{
    /// <summary>
    /// Acts as a high-capacity, generic immutable container for storing final serialized or formatted postprocessing outputs.
    /// </summary>
    /// <typeparam name="TValue">The underlying unmanaged data type of values stored in the postprocess buffer.</typeparam>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps serialized data or binary streams tightly aligned 
    /// inside unmanaged memory buffers to ensure maximal CPU cache locality, rapid linear traversal, and optimal streaming throughput.
    /// <para/>
    /// Because it provides direct raw pointer access, downstream execution components can iterate over finalized target blocks 
    /// with zero bounds-checking overhead, completely bypassing the managed heap. Unlike classic pooled collections, this structure 
    /// does not own the underlying memory and does not require explicit finalization via <c>Dispose</c>. Its entire lifecycle is bound to 
    /// the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct PostprocessingResult<TValue> where TValue : unmanaged
    {
        private readonly TValue* _buffer;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="PostprocessingResult{TValue}"/> structure using a direct unmanaged pointer to the generated output block.
        /// </summary>
        /// <param name="buffer">A direct unmanaged pointer to the start of the contiguous postprocessed value buffer in memory.</param>
        /// <param name="length">The total number of fully calculated and valid postprocessed elements contained within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PostprocessingResult(TValue* buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded postprocessing values.
        /// </summary>
        /// <value>A raw pointer of type <see cref="TValue"/>* pointing directly to the first element in the arena allocation block.</value>
        public readonly TValue* BufferPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer;
        }

        /// <summary>
        /// Gets the total number of valid elements currently recorded in the postprocess container.
        /// </summary>
        /// <value>An integer tracking the exact element count available for linear iteration and streaming.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the accumulated postprocessing values.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<TValue> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_buffer, _length);
        }

        /// <summary>
        /// Retrieves a postprocessed value at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the value within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="TValue"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        public readonly ref readonly TValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
                return ref _buffer[index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the PostprocessResult.");
    }
}
