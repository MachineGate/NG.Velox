namespace NG.Velox.Tests.Preprocessor
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Memory.Core;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [TestClass]
    public class VeloxPreprocessorTests
    {
        private const int DEFAULT_ARENA_SIZE = 1024 * 1024; // 1 МБ

        [TestMethod]
        public void Process_EmptyInput_ReturnsEmptyResult()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = ReadOnlyMemory<char>.Empty;
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual(0, result.Length);
                Assert.IsTrue(result.Text.IsEmpty);
                Assert.IsFalse(diag.HasErrors);
                Assert.IsFalse(diag.HasWarnings);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_CopiesIntoArena_IsolatesResultFromSourceBuffer()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                char[] sourceBuffer = "G01X10".ToCharArray();
                ReadOnlyMemory<char> input = sourceBuffer.AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;
                Assert.AreEqual("G01X10", new string(result.Text));

                sourceBuffer[0] = 'M';
                sourceBuffer[1] = '3';
                sourceBuffer[2] = '0';

                Assert.AreEqual("G01X10", new string(result.Text));
                Assert.AreEqual(sourceBuffer.Length, result.Length);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_MultiLineParenthesesComment_PreservesNewlinesInText()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "(a\nb\nc)\nG01X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;
                ReadOnlySpan<char> text = result.Text;

                int newlineCount = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n') newlineCount++;
                }

                Assert.AreEqual(3, newlineCount);
                Assert.AreEqual(input.Length, result.Length);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
