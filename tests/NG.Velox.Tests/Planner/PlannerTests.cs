namespace NG.Velox.Tests.Planner
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Helpers;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Planning.Core;
    using NG.Velox.Planning.Data;

    [TestClass]
    public class VeloxPlannerTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public void Plan_SingleLinearMove_CalculatesCorrectLength()
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

                unsafe
                {
                    Frame* framePtr = ArenaAllocator.Allocate<Frame>(ref arena, 1);
                    MachineFrame* machineFramePtr = ArenaAllocator.Allocate<MachineFrame>(ref arena, 1);

                    *framePtr = frame;
                    *machineFramePtr = machineFrame;

                    InterpretationResult intResult = new(framePtr, machineFramePtr, 1);
                    
                    context.InterpretationResult = intResult;

                    new VeloxPlanner().Process(ref context, ref diag);

                    PlanningResult planResult = context.PlanningResult;

                    Assert.AreEqual(1, planResult.Length);

                    ref readonly PlannedBlock block = ref planResult[0];

                    Assert.AreEqual(10.0, block.Length, 0.001);
                    Assert.AreEqual(0.0, block.VEntry);
                    Assert.AreEqual(0.0, block.VExit);
                    Assert.IsFalse(diag.HasErrors);
                }
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
