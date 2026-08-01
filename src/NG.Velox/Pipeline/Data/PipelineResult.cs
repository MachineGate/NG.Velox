using System.Runtime.CompilerServices;

namespace NG.Velox.Pipeline.Data
{
    /// <summary>
    /// Represents a strictly immutable, zero-allocation unmanaged snapshot containing the final compiled pipeline output elements.
    /// Allocated entirely within a transient <see cref="Memory.Core.MemoryArena"/>, this structure holds direct raw pointers 
    /// to ensure maximum data locality and zero bounds-checking overhead for downstream layers.
    /// </summary>
    /// <typeparam name="TOutput">The underlying unmanaged, blittable data type compiled by the final postprocessing stage.</typeparam>
    /// <remarks>
    /// This is a stack-only <see langword="ref struct"/> view over the arena-allocated memory block. It does not own the underlying resources 
    /// and does not require explicit finalization via <c>Dispose</c>. Its entire lifecycle is strictly bound to the lifespan 
    /// of the parent <see cref="Memory.Core.MemoryArena"/> instance.
    /// </remarks>
    public readonly unsafe ref struct PipelineResult<TOutput> where TOutput : unmanaged
    {
        private readonly TOutput* _buffer;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="PipelineResult{TOutput}"/> structure using a direct unmanaged pointer.
        /// </summary>
        /// <param name="buffer">A direct unmanaged pointer to the start of the contiguous output buffer inside the arena.</param>
        /// <param name="length">The total number of valid elements compiled within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PipelineResult(TOutput* buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded pipeline output values.
        /// </summary>
        /// <value>A raw pointer of type <see cref="TOutput"/>* pointing directly to the first element in the arena allocation block.</value>
        public readonly TOutput* BufferPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer;
        }

        /// <summary>
        /// Gets the total number of compiled elements currently recorded in the container.
        /// </summary>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only span view spanning across the accumulated unmanaged output values.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<TOutput> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_buffer, _length);
        }

        /// <summary>
        /// Retrieves a compiled element at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the element within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="TOutput"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        public readonly ref readonly TOutput this[int index]
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
            throw new IndexOutOfRangeException("Index was out of range of the PipelineResult.");
    }
}