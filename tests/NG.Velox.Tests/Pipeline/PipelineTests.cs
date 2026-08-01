namespace NG.Velox.Tests.Pipeline
{
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Factories;
    using NG.Velox.Memory.Core;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Pipeline.Extensions;
    using NG.Velox.Pipeline.Interfaces;

    [TestClass]
    public class VeloxPipelineTests
    {
        private const int DEFAULT_ARENA_SIZE = 4 * 1024 * 1024; 

        [TestMethod]
        public void Process_ValidGCode_ProducesBinaryOutput()
        {
            IVeloxPipeline<ReadOnlyMemory<char>, byte> pipeline = VeloxPipelineFactory.CreateHardware();
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 X10 Y10 F100".AsMemory();
                PipelineResult<byte> unmanagedResult = pipeline.Process(input, ref arena, ref diag);

                Assert.IsFalse(diag.HasErrors);

                Assert.IsGreaterThan(0, unmanagedResult.Length);

                byte[] managedArray = unmanagedResult.ToArray();
                Assert.IsNotEmpty(managedArray);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_InvalidGCode_ReturnsErrorsAndEmptyOutput()
        {
            IVeloxPipeline<ReadOnlyMemory<char>, byte> pipeline = VeloxPipelineFactory.CreateHardware();
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 @".AsMemory();
                PipelineResult<byte> unmanagedResult = pipeline.Process(input, ref arena, ref diag);

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(0, unmanagedResult.Length);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
