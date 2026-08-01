using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NG.Velox.Tests.Postprocessor
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Helpers;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Postprocessing.Core;
    using NG.Velox.Postprocessing.Data;

    [TestClass]
    public class VeloxPostprocessorTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public void Process_SingleSegment_WritesCorrectBinaryLayout()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new ();

                ReadOnlyMemory<char> input = ReadOnlyMemory<char>.Empty;
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                MachineFrame mFrame = new(42);
                Frame frame = new(
                    x: 1.0, y: 2.0, z: 3.0,
                    i: 0, j: 0, k: 0, r: 0,
                    feedRate: 100.0,
                    activeMotionMode: 1,
                    activePlane: 17,
                    isAbsoluteDistance: true,
                    hasMotion: true,
                    hasArcParams: false);

                TrajectoryPoint point = new(1.0, 2.0, 3.0, 10.0, 0.1);
                TrajectorySegment segment = new(frameIndex: 0, startIndex: 0, count: 1);

                unsafe
                {
                    Frame* framePtr = ArenaAllocator.Allocate<Frame>(ref arena, 1);
                    MachineFrame* machineFramePtr = ArenaAllocator.Allocate<MachineFrame>(ref arena, 1);
                    *framePtr = frame;
                    *machineFramePtr = mFrame;
                    InterpretationResult interpretationResult = new(framePtr, machineFramePtr, 1);

                    TrajectoryPoint* pointPtr = (TrajectoryPoint*)arena.Allocate(sizeof(TrajectoryPoint), Unsafe.SizeOf<TrajectoryPoint>());
                    TrajectorySegment* segmentPtr = (TrajectorySegment*)arena.Allocate(sizeof(TrajectorySegment), Unsafe.SizeOf<TrajectorySegment>());
                    *pointPtr = point;
                    *segmentPtr = segment;
                    var interpolationResult = new InterpolationResult(pointPtr, 1, segmentPtr, 1);

                    context.InterpretationResult = interpretationResult;
                    context.InterpolationResult = interpolationResult;

                    new VeloxHardwarePostprocessor().Process(ref context, ref diag);

                    PostprocessingResult<byte> postResult = context.PostprocessingResult;

                    Assert.AreEqual(76, postResult.Length);

                    var writtenFrame = MemoryMarshal.Read<MachineFrame>(postResult.Values);
                    Assert.AreEqual(42u, writtenFrame.MachineFlags);

                    int count = MemoryMarshal.Read<int>(postResult.Values.Slice(32));
                    Assert.AreEqual(1, count);

                    var writtenPoint = MemoryMarshal.Read<TrajectoryPoint>(postResult.Values.Slice(36));
                    Assert.AreEqual(1.0, writtenPoint.X);
                    Assert.AreEqual(2.0, writtenPoint.Y);
                    Assert.AreEqual(3.0, writtenPoint.Z);
                }
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
