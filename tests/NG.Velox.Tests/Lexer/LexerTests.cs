namespace NG.Velox.Tests.Lexer
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Lexing.Core;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [TestClass]
    public class VeloxLexerTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024;

        [TestMethod]
        public void Tokenize_EmptyInput_ReturnsZeroTokens()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = ReadOnlyMemory<char>.Empty;
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                
                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult result = context.LexingResult;

                Assert.AreEqual(0, result.Length);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Tokenize_SimpleGCode_ReturnsCorrectTokens()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);

                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult result = context.LexingResult;

                Assert.AreEqual(4, result.Length);
                Assert.AreEqual(TokenKind.Address, result[0].Kind);
                Assert.AreEqual(TokenKind.Number, result[1].Kind);
                Assert.AreEqual(TokenKind.Address, result[2].Kind);
                Assert.AreEqual(TokenKind.Number, result[3].Kind);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Tokenize_UnknownSymbol_ReturnsError101()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 @".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);

                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult result = context.LexingResult;

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(101, diag.Diagnostics[0].Code);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Tokenize_CommentsAndSpaces_AreIgnoredButIndexMapped()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 (comment) @".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);

                PreprocessingResult preResult = context.PreprocessingResult;
                LexingResult result = context.LexingResult;

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(101, diag.Diagnostics[0].Code);

                Assert.AreEqual(14, diag.Diagnostics[0].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
