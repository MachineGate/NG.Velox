using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NG.Velox.Postprocessing.Data
{
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;

    /// <summary>
    /// Represents a dense, blittable unmanaged Data Transfer Object tracking simulation boundaries.
    /// Instead of owning heap-allocated arrays, it stores direct pointers to existing points inside the arena.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct SimulationFrame
    {
        public readonly MachineFrame MachineFrame;
        public readonly TrajectoryPoint* Points;
        public readonly int PointsCount;
        public readonly byte MotionMode;

        /// <summary>
        /// Initializes a new instance of the unmanaged <see cref="SimulationFrame"/> structure.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SimulationFrame(MachineFrame machineFrame, TrajectoryPoint* points, int pointsCount, byte motionMode)
        {
            MachineFrame = machineFrame;
            Points = points;
            PointsCount = pointsCount;
            MotionMode = motionMode;
        }

        /// <summary>
        /// Gets a direct read-only view spanning across this specific frame's trajectory points.
        /// </summary>
        public readonly ReadOnlySpan<TrajectoryPoint> FramePoints
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Points, PointsCount);
        }
    }
}