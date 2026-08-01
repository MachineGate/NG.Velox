namespace NG.Velox.Tests.Parser
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Lexing.Core;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Parsing.Core;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [TestClass]
    public class VeloxParserTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public void Parse_ValidTokens_ReturnsCorrectNodes()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 X10.5".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult lexResult = context.LexingResult;
                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);

                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(CommandKind.G, parseResult[0].CommandKind);
                Assert.AreEqual(1.0, parseResult[0].Value);

                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);
                Assert.AreEqual(CoordinateKind.X, parseResult[1].CoordinateKind);
                Assert.AreEqual(10.5, parseResult[1].Value);

                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_MissingNumber_ReturnsError202()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 X".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult lexResult = context.LexingResult;
                ParsingResult parseResult = context.ParsingResult;

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(202, diag.Diagnostics[0].Code);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
