namespace NG.Velox.Tests.Preprocessor
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Memory.Core;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [TestClass]
    public class PreprocessorTests
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
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_SimpleText_ReturnsIdenticalTextAndIndices()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual("G01X10", new string(result.Text));
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.AreEqual(i, result.IndexMap[i]);
                }
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_ParenthesesComments_RemovesAndMapsIndices()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 (comment) X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual("G01X10", new string(result.Text));

                Assert.AreEqual(0, result.IndexMap[0]);  // 'G'
                Assert.AreEqual(1, result.IndexMap[1]);  // '0'
                Assert.AreEqual(2, result.IndexMap[2]);  // '1'
                Assert.AreEqual(14, result.IndexMap[3]); // 'X'
                Assert.AreEqual(15, result.IndexMap[4]); // '1'
                Assert.AreEqual(16, result.IndexMap[5]); // '0'
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_SemicolonComments_RemovesButKeepsNewlines()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 ;comment\nX10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual("G01\nX10", new string(result.Text));

                Assert.AreEqual(0, result.IndexMap[0]);  // 'G'
                Assert.AreEqual(1, result.IndexMap[1]);  // '0'
                Assert.AreEqual(2, result.IndexMap[2]);  // '1'
                Assert.AreEqual(12, result.IndexMap[3]); // '\n'
                Assert.AreEqual(13, result.IndexMap[4]); // 'X'
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_NCodes_RemovesLineNumbers()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "N10 G01 N20X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual("G01X10", new string(result.Text));
                Assert.AreEqual(4, result.IndexMap[0]);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_Whitespaces_RemovesSpacesAndTabsKeepsNewlines()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01 \t X10\r\nY20".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                Assert.AreEqual("G01X10\nY20", new string(result.Text));
                Assert.AreEqual('\n', result.Text[6]);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Process_ComplexMix_HandlesAllRulesCorrectly()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "N5 G90 G17 (comment) ; Milling\n N10 G01 X10.5 Y20 F100".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);

                PreprocessingResult result = context.PreprocessingResult;

                string cleanText = new(result.Text);
                Assert.AreEqual("G90G17\nG01X10.5Y20F100", cleanText);

                int newlineIndex = cleanText.IndexOf('\n');
                Assert.IsGreaterThan(0, newlineIndex);
                Assert.AreEqual('\n', result.Text[newlineIndex]);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
