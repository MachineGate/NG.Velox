namespace NG.Velox.Tests.Interpolator
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Helpers;
    using NG.Velox.Interpolation.Core;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Planning.Core;

    [TestClass]
    public class VeloxInterpolatorTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public unsafe void Interpolate_SingleBlock_GeneratesPointsAndSegments()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = ReadOnlyMemory<char>.Empty;
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                Frame frame = new(
                x: 0.0, y: 10.0, z: 0.0,
                i: 0.0, j: 0.0, k: 0.0, r: 0.0,
                feedRate: 100.0,
                activeMotionMode: 1,
                activePlane: 17,
                isAbsoluteDistance: true,
                hasMotion: true,
                hasArcParams: false);

                MachineFrame machineFrame = new(0);

                Frame* framePtr = ArenaAllocator.Allocate<Frame>(ref arena, 1);
                MachineFrame* machineFramePtr = ArenaAllocator.Allocate<MachineFrame>(ref arena, 1);

                *framePtr = frame;
                *machineFramePtr = machineFrame;

                InterpretationResult interpretationResult = new(framePtr, machineFramePtr, 1);

                context.InterpretationResult = interpretationResult;

                new VeloxPlanner().Process(ref context, ref diag);
                new VeloxInterpolator().Process(ref context, ref diag);

                InterpolationResult interpolationResult = context.InterpolationResult;

                Assert.IsGreaterThan(0, interpolationResult.PointsCount);
                Assert.AreEqual(1, interpolationResult.SegmentsCount);

                ref readonly TrajectorySegment segment = ref interpolationResult.GetSegment(0);

                Assert.AreEqual(0, segment.FrameIndex);
                Assert.AreEqual(0, segment.StartIndex);
                Assert.AreEqual(interpolationResult.PointsCount, segment.Count);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
