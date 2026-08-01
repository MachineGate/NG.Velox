using System.Runtime.CompilerServices;

namespace NG.Velox.Interpolation.Data
{
    /// <summary>
    /// Encapsulates the multi-segment trajectory output generated during motion interpolation using raw unmanaged pointers.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous parallel memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps trajectory points and segments tightly aligned 
    /// inside unmanaged memory buffers to prevent managed heap allocations, reduce CPU L1/L2 cache misses during high-density 
    /// step generation, and ensure rapid data streaming to hardware controllers.
    /// <para/>
    /// Because it provides direct raw pointer access, downstream execution components can iterate over time-slices and fine 
    /// interpolation vectors with zero bounds-checking overhead. Unlike classic pooled collections, this structure does not own 
    /// the underlying memory and does not require explicit finalization via <c>Dispose</c>. Its entire lifecycle is bound to 
    /// the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct InterpolationResult
    {
        private readonly TrajectoryPoint* _points;
        private readonly TrajectorySegment* _segments;
        private readonly int _pointsCount;
        private readonly int _segmentsCount;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="InterpolationResult"/> structure using direct unmanaged pointers to the generated trajectory data.
        /// </summary>
        /// <param name="points">A direct unmanaged pointer to the start of the contiguous fine-interpolation trajectory points buffer.</param>
        /// <param name="pointsCount">The actual number of valid trajectory points written to the points buffer.</param>
        /// <param name="segments">A direct unmanaged pointer to the start of the contiguous trajectory kinematic segments buffer.</param>
        /// <param name="segmentsCount">The actual number of valid trajectory segments written to the segments buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpolationResult(TrajectoryPoint* points, int pointsCount, TrajectorySegment* segments, int segmentsCount)
        {
            _points = points;
            _pointsCount = pointsCount;
            _segments = segments;
            _segmentsCount = segmentsCount;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded trajectory point collection.
        /// </summary>
        /// <value>A raw pointer of type <see cref="TrajectoryPoint"/>* pointing directly to the first element in the arena allocation block.</value>
        public readonly TrajectoryPoint* PointsPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _points;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the recorded trajectory kinematic segment collection.
        /// </summary>
        /// <value>A raw pointer of type <see cref="TrajectorySegment"/>* pointing directly to the first element in the arena allocation block.</value>
        public readonly TrajectorySegment* SegmentsPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segments;
        }

        /// <summary>
        /// Gets the actual number of valid trajectory points written to the buffer.
        /// </summary>
        /// <value>An integer tracking the exact point count available for linear iteration and step streaming.</value>
        public readonly int PointsCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pointsCount;
        }

        /// <summary>
        /// Gets the actual number of valid trajectory segments written to the buffer.
        /// </summary>
        /// <value>An integer tracking the exact kinematic segment count available for evaluation.</value>
        public readonly int SegmentsCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segmentsCount;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the recorded trajectory points.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena points buffer.</value>
        public readonly ReadOnlySpan<TrajectoryPoint> Points
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_points, _pointsCount);
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the recorded trajectory segments.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena segments buffer.</value>
        public readonly ReadOnlySpan<TrajectorySegment> Segments
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_segments, _segmentsCount);
        }

        /// <summary>
        /// Retrieves a trajectory point at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the point within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="TrajectoryPoint"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="PointsCount"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly TrajectoryPoint GetPoint(int index)
        {
            if ((uint)index >= (uint)_pointsCount) ThrowPointsIndexOutOfRange();
            return ref _points[index];
        }

        /// <summary>
        /// Retrieves a trajectory kinematic segment at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the segment within the contiguous memory sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="TrajectorySegment"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="SegmentsCount"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly TrajectorySegment GetSegment(int index)
        {
            if ((uint)index >= (uint)_segmentsCount) ThrowSegmentsIndexOutOfRange();
            return ref _segments[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowPointsIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the trajectory points buffer.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSegmentsIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the trajectory segments buffer.");
    }
}
