using System.Runtime.InteropServices;

namespace NG.Velox.Interpolation.Data
{
    /// <summary>
    /// Maps a continuous slice of trajectory points within a flat array to their source coordinate frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Memory Optimization:</b> Instead of storing a frame reference inside every individual trajectory point, 
    /// points are stored in a contiguous flat array. This structure acts as a "table of contents" entry, marking the boundaries 
    /// where <c>Points[StartIndex..StartIndex+Count)</c> belong to <c>FrameIndex</c>. This approach reduces 
    /// metadata overhead by approximately 50%.
    /// </para>
    /// <para>
    /// This structure enforces a strict <see cref="LayoutKind.Sequential"/> memory layout with 4-byte alignment, 
    /// making it highly optimized for binary caching, hardware streaming, and quick sequential iteration.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct TrajectorySegment
    {
        /// <summary>
        /// The unique index identifying the source system frame or machine coordinate frame.
        /// </summary>
        public readonly int FrameIndex;

        /// <summary>
        /// The zero-based starting index of the associated points inside the global contiguous array.
        /// </summary>
        public readonly int StartIndex;

        /// <summary>
        /// The total number of consecutive trajectory points contained within this segment.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectorySegment"/> structure with specified frame mapping bounds.
        /// </summary>
        /// <param name="frameIndex">The index of the system frame.</param>
        /// <param name="startIndex">The global zero-based starting index within the flat trajectory buffer.</param>
        /// <param name="count">The number of points belonging to this frame segment.</param>
        public TrajectorySegment(int frameIndex, int startIndex, int count)
        {
            FrameIndex = frameIndex;
            StartIndex = startIndex;
            Count = count;
        }
    }
}
