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

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);

                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(CommandKind.G, parseResult[0].CommandKind);
                Assert.AreEqual(1.0, parseResult[0].Value);
                Assert.AreEqual(0, parseResult[0].Start);
                Assert.AreEqual(3, parseResult[0].Length);

                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);
                Assert.AreEqual(CoordinateKind.X, parseResult[1].CoordinateKind);
                Assert.AreEqual(10.5, parseResult[1].Value);
                Assert.AreEqual(4, parseResult[1].Start);
                Assert.AreEqual(5, parseResult[1].Length);

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

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(202, diag.Diagnostics[0].Code);

                Assert.AreEqual(4, diag.Diagnostics[0].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_EmptyInput_ReturnsZeroNodes()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = ReadOnlyMemory<char>.Empty;
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(0, parseResult.Length);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_OnlyTrivia_ReturnsZeroNodes()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "(comment)\n;another\n".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(0, parseResult.Length);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_TwoBlocks_EmitsNewLineBetweenThem()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\nX10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(3, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[2].Kind);

                Assert.AreEqual(3, parseResult[1].Start);
                Assert.AreEqual(1, parseResult[1].Length);

                Assert.AreEqual(4, parseResult[2].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_EmptyLineBetweenBlocks_CollapsesToSingleNewLine()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\n\n\nX10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(3, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[2].Kind);

                Assert.AreEqual(3, parseResult[1].Start);
                Assert.AreEqual(6, parseResult[2].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_LeadingEmptyLines_NoNewLineNode()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "\n\n\nG01X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);

                Assert.AreEqual(3, parseResult[0].Start);
                Assert.AreEqual(6, parseResult[1].Start);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_TrailingNewline_EmitsNewLineAfterContent()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\n".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);
                Assert.AreEqual(3, parseResult[1].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_TrailingMultipleNewlines_OnlyOneNewLineNode()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\n\n\n".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);
                Assert.AreEqual(3, parseResult[1].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_CommentOnlyLine_NoNewLineNode()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = ";header comment\nG01X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);

                Assert.AreEqual(16, parseResult[0].Start);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_MultiLineCommentInsideBlock_NoBoundary()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01(a\nb\nc)X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);

                Assert.AreEqual(10, parseResult[1].Start);
                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_MultiLineCommentBetweenBlocks_SingleBoundary()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\n(a\nb\nc)\nX10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(3, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);
                Assert.AreEqual(NodeKind.Coordinate, parseResult[2].Kind);

                Assert.AreEqual(3, parseResult[1].Start);
                Assert.AreEqual(12, parseResult[2].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_DirectAbsolutePositions_NoIndexMapRequired()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "  G01 (c) X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);

                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(2, parseResult[0].Start);
                Assert.AreEqual(3, parseResult[0].Length);

                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);
                Assert.AreEqual(10, parseResult[1].Start);
                Assert.AreEqual(3, parseResult[1].Length);

                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_Error201_NumberWithoutAddress_DirectPosition()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\n10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                Assert.IsTrue(diag.HasErrors);
                Assert.AreEqual(201, diag.Diagnostics[0].Code);

                Assert.AreEqual(4, diag.Diagnostics[0].Start);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_NodeLengthReflectsOriginalSpan()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G 01".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(1, parseResult.Length);
                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(1.0, parseResult[0].Value);

                Assert.AreEqual(0, parseResult[0].Start);
                Assert.AreEqual(4, parseResult[0].Length);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_NCode_SkippedWithoutAffectingPositions()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "N10 G01X10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(2, parseResult.Length);

                Assert.AreEqual(NodeKind.Command, parseResult[0].Kind);
                Assert.AreEqual(4, parseResult[0].Start);

                Assert.AreEqual(NodeKind.Coordinate, parseResult[1].Kind);
                Assert.AreEqual(7, parseResult[1].Start);

                Assert.IsFalse(diag.HasErrors);
            }
            finally
            {
                arena.Dispose();
            }
        }

        [TestMethod]
        public void Parse_NewLineNode_HasCorrectStartAndLength()
        {
            MemoryArena arena = new(DEFAULT_ARENA_SIZE);
            try
            {
                DiagnosticBag diag = new();
                ReadOnlyMemory<char> input = "G01\r\nX10".AsMemory();
                VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref input, ref arena);

                new VeloxMemoryPreprocessor().Process(ref context, ref diag);
                new VeloxLexer().Process(ref context, ref diag);
                new VeloxParser().Process(ref context, ref diag);

                ParsingResult parseResult = context.ParsingResult;

                Assert.AreEqual(3, parseResult.Length);
                Assert.AreEqual(NodeKind.NewLine, parseResult[1].Kind);

                Assert.AreEqual(4, parseResult[1].Start);
                Assert.AreEqual(1, parseResult[1].Length);

                Assert.AreEqual(0.0, parseResult[1].Value);
            }
            finally
            {
                arena.Dispose();
            }
        }
    }
}
