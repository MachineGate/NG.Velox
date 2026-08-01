using System.Runtime.CompilerServices;

namespace NG.Velox.Planning.Data
{
    /// <summary>
    /// Acts as a high-performance, zero-allocation container for storing acceleration-bounded kinematic segments generated during look-ahead trajectory planning.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable-holder, stack-only <see langword="ref struct"/> view over contiguous memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps motion blocks tightly aligned inside a contiguous memory array 
    /// of <see cref="PlannedBlock"/> elements to ensure maximal CPU cache locality, rapid multi-pass velocity planning, and seamless path optimization.
    /// <para/>
    /// Because it provides direct raw pointer access and stack-bound <see cref="Span{T}"/> slices, the look-ahead planning engine can execute 
    /// bidirectional in-place velocity profile modifications with zero bounds-checking overhead, completely bypassing the managed heap. 
    /// Unlike classic pooled collections, this structure does not own the underlying memory and does not require explicit finalization via <c>Dispose</c>. 
    /// Its entire lifecycle is bound to the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct PlanningResult
    {
        private readonly PlannedBlock* _blocks;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="PlanningResult"/> structure using a direct unmanaged pointer to the generated kinematic block sequence.
        /// </summary>
        /// <param name="blocks">A direct unmanaged pointer to the start of the contiguous kinematic motion block buffer in memory.</param>
        /// <param name="length">The total number of fully calculated and valid kinematic blocks contained within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanningResult(PlannedBlock* blocks, int length)
        {
            _blocks = blocks;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded kinematic motion block collection.
        /// </summary>
        /// <value>A raw pointer of type <see cref="PlannedBlock"/>* pointing directly to the first layout slot inside the arena allocation block.</value>
        public readonly PlannedBlock* BlocksPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _blocks;
        }

        /// <summary>
        /// Gets the total number of fully calculated and valid kinematic blocks recorded within this syntax sequence.
        /// </summary>
        /// <value>An integer tracking the exact element count available for linear iteration and kinematic processing.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Retrieves a kinematic motion block at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the planned block within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="PlannedBlock"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        public readonly ref readonly PlannedBlock this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
                return ref _blocks[index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the PlanningResult.");
    }
}
