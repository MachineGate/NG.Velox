namespace NG.Velox.Tests.Interpreter
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpretation.Core;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Lexing.Core;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Parsing.Core;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [TestClass]
    public class VeloxInterpreterTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public void Interpret_SimpleMove_CreatesFrameWithMotion()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 X10 Y20 F100".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);
                new VeloxInterpreter().Process(ref context, ref diag);

                PreprocessingResult prepResult = context.PreprocessingResult;
                LexingResult lexResult = context.LexingResult;
                ParsingResult parseResult = context.ParsingResult;
                InterpretationResult intResult = context.InterpretationResult;

                Assert.AreEqual(1, intResult.Length);

                ref readonly Frame frame = ref intResult.GetFrame(0);

                Assert.IsTrue(frame.HasMotion);
                Assert.AreEqual(10.0, frame.X);
                Assert.AreEqual(20.0, frame.Y);
                Assert.AreEqual(1, frame.MotionMode); // G01
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Interpret_MCode_SetsMachineFlags()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "M00".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);
                new VeloxInterpreter().Process(ref context, ref diag);

                PreprocessingResult prepResult = context.PreprocessingResult;
                LexingResult lexResult = context.LexingResult;
                ParsingResult parseResult = context.ParsingResult;
                InterpretationResult intResult = context.InterpretationResult;

                Assert.AreEqual(1, intResult.Length);

                ref readonly MachineFrame machineFrame = ref intResult.GetMachineFrame(0);

                Assert.AreNotEqual((uint)0, machineFrame.MachineFlags & 1); // M00
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
