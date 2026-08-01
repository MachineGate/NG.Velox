using System.Runtime.CompilerServices;

namespace NG.Velox.Interpretation.Data
{
    /// <summary>
    /// Acts as a high-capacity, zero-allocation immutable container for parallel tracking of interpreted G-code blocks and hardware metadata state.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous parallel memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps data tightly aligned in two parallel contiguous arrays 
    /// to prevent managed heap allocations, reduce cache misses during look-ahead lookups, and ensure rapid data streaming to downstream systems.
    /// <para/>
    /// Because it provides direct raw pointer access, downstream execution components or hardware streaming providers can iterate over runtime frames 
    /// with zero bounds-checking overhead, completely bypassing the managed heap. Unlike classic pooled collections, this structure does not own 
    /// the underlying memory and does not require explicit finalization via <c>Dispose</c>. Its entire lifecycle is bound to the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct InterpretationResult
    {
        private readonly Frame* _frames;
        private readonly MachineFrame* _machineFrames;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="InterpretationResult"/> structure using direct unmanaged pointers to the generated parallel frames.
        /// </summary>
        /// <param name="frames">A direct unmanaged pointer to the start of the contiguous internal virtual machine configuration dataset.</param>
        /// <param name="machineFrames">A direct unmanaged pointer to the start of the parallel generated hardware serialization payloads.</param>
        /// <param name="length">The total number of fully populated and verified parallel blocks contained within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpretationResult(Frame* frames, MachineFrame* machineFrames, int length)
        {
            _frames = frames;
            _machineFrames = machineFrames;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded internal virtual machine configuration dataset.
        /// </summary>
        /// <value>A raw pointer of type <see cref="Frame"/>* pointing directly to the first layout slot inside the arena allocation block.</value>
        public readonly Frame* FramesPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _frames;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the generated hardware serialization payloads.
        /// </summary>
        /// <value>A raw pointer of type <see cref="MachineFrame"/>* pointing directly to the first layout slot inside the arena allocation block.</value>
        public readonly MachineFrame* MachineFramesPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _machineFrames;
        }

        /// <summary>
        /// Gets the total number of fully populated and verified parallel blocks recorded within this interpretation snapshot.
        /// </summary>
        /// <value>An integer tracking the exact element count available for linear iteration and streaming.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the recorded internal virtual machine configuration dataset.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<Frame> Frames
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_frames, _length);
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the generated hardware serialization payloads.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<MachineFrame> MachineFrames
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_machineFrames, _length);
        }

        /// <summary>
        /// Retrieves an internal virtual machine execution frame at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the frame within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="Frame"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly Frame GetFrame(int index)
        {
            if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
            return ref _frames[index];
        }

        /// <summary>
        /// Retrieves a hardware-ready machine execution frame at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the machine frame within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="MachineFrame"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly MachineFrame GetMachineFrame(int index)
        {
            if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
            return ref _machineFrames[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the InterpretationResult.");
    }
}
