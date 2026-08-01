using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace NG.Velox.Benchmarks
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpolation.Core;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Core;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Lexing.Core;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Parsing.Core;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Planning.Core;
    using NG.Velox.Planning.Data;
    using NG.Velox.Postprocessing.Core;
    using NG.Velox.Preprocessing.Core;
    using NG.Velox.Preprocessing.Data;

    [MemoryDiagnoser, CPUUsageDiagnoser]
    public class LayersBenchmarks
    {
        private VeloxMemoryPreprocessor _preprocessor;
        private VeloxLexer _lexer;
        private VeloxParser _parser;
        private VeloxInterpreter _interpreter;
        private VeloxPlanner _planner;
        private VeloxInterpolator _interpolator;
        private VeloxHardwarePostprocessor _postprocessor;

        private ReadOnlyMemory<char> _gCodeText;

        private MemoryArena _setupArena;

        private MemoryArena _runtimeArena;
        private DiagnosticBag _runtimeBag;

        private unsafe char* _preprocessedTextPtr;
        private unsafe int* _preprocessedIndexMapPtr;
        private int _preprocessedLength;

        private unsafe Token* _lexedTokensPtr;
        private int _lexedLength;

        private unsafe Node* _parsedNodesPtr;
        private int _parsedLength;

        private unsafe Frame* _interpretatedFramesPtr;
        private unsafe MachineFrame* _interpretatedMachineFramesPtr;
        private int _interpretatedLength;

        private unsafe PlannedBlock* _plannedBlocksPtr;
        private int _plannedLength;

        private unsafe TrajectoryPoint* _interpolatedPointsPtr;
        private unsafe TrajectorySegment* _interpolatedSegmentsPtr;
        private int _interpolatedPointsCount;
        private int _interpolatedSegmentsCount;

        [Params(10, 100, 1000, 10000, 100000)]
        public int LineCount { get; set; }

        [GlobalSetup]
        public unsafe void Setup()
        {
            if (_setupArena.Capacity > 0)
            {
                _setupArena.Dispose();
            }

            _preprocessor = new VeloxMemoryPreprocessor();
            _lexer = new VeloxLexer();
            _parser = new VeloxParser();
            _interpreter = new VeloxInterpreter();
            _planner = new VeloxPlanner();
            _interpolator = new VeloxInterpolator();
            _postprocessor = new VeloxHardwarePostprocessor();

            _gCodeText = GenerateGCode(LineCount).AsMemory();

            _setupArena = new MemoryArena(1800 * 1024 * 1024);
            DiagnosticBag setupDiag = new();
            setupDiag.EnsureCapacity(1);

            VeloxContext<ReadOnlyMemory<char>, byte> context = new (ref _gCodeText, ref _setupArena);

            _preprocessor.Process(ref context, ref setupDiag);
            _lexer.Process(ref context, ref setupDiag);
            _parser.Process(ref context, ref setupDiag);
            _interpreter.Process(ref context, ref setupDiag);
            _planner.Process(ref context, ref setupDiag);
            _interpolator.Process(ref context, ref setupDiag);

            PreprocessingResult preprocessed = context.PreprocessingResult;
            _preprocessedTextPtr = preprocessed.TextPtr;
            _preprocessedIndexMapPtr = preprocessed.IndexMapPtr;
            _preprocessedLength = preprocessed.Length;

            LexingResult lexed = context.LexingResult;
            _lexedTokensPtr = lexed.Tokens;
            _lexedLength = lexed.Length;

            ParsingResult parsed = context.ParsingResult;
            _parsedNodesPtr = parsed.NodesPtr;
            _parsedLength = parsed.Length;

            InterpretationResult interpreted = context.InterpretationResult;
            _interpretatedFramesPtr = interpreted.FramesPtr;
            _interpretatedMachineFramesPtr = interpreted.MachineFramesPtr;
            _interpretatedLength = interpreted.Length;

            PlanningResult planned = context.PlanningResult;
            _plannedBlocksPtr = planned.BlocksPtr;
            _plannedLength = planned.Length;

            InterpolationResult interpolated = context.InterpolationResult;
            _interpolatedPointsPtr = interpolated.PointsPtr;
            _interpolatedSegmentsPtr = interpolated.SegmentsPtr;
            _interpolatedPointsCount = interpolated.PointsCount;
            _interpolatedSegmentsCount = interpolated.SegmentsCount;

            _runtimeArena = new MemoryArena(1800 * 1024 * 1024);
            _runtimeBag = new DiagnosticBag();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _runtimeArena.Dispose();
            _runtimeBag.Dispose();
            _setupArena.Dispose();
        }

        private static string GenerateGCode(int lineCount)
        {
            var sb = new StringBuilder(lineCount * 30);
            sb.AppendLine("G90 G00 X0 Y0 F600");
            for (int i = 0; i < lineCount - 1; i++)
            {
                if (i % 5 == 0) sb.AppendLine("G02 X10 Y0 I5 J0");
                else sb.AppendLine($"G01 X{i % 100} Y{i % 50} Z{i % 10}");
            }
            return sb.ToString();
        }

        [Benchmark(Description = "0. Preprocessor (Cleaning & IndexMap)")]
        public void Layer0_Preprocessor()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena);

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _preprocessor.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "1. Lexer (Tokenization)")]
        public unsafe void Layer1_Lexer()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                PreprocessingResult = new PreprocessingResult(_preprocessedTextPtr, _preprocessedIndexMapPtr, _preprocessedLength)
            };

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _lexer.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "2. Parser - AST Nodes")]
        public unsafe void Layer2_Parser()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                PreprocessingResult = new PreprocessingResult(_preprocessedTextPtr, _preprocessedIndexMapPtr, _preprocessedLength),
                LexingResult = new LexingResult(_lexedTokensPtr, _lexedLength)
            };

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _parser.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "3. Interpreter - VM & MachineFrames")]
        public unsafe void Layer3_Interpreter()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                PreprocessingResult = new PreprocessingResult(_preprocessedTextPtr, _preprocessedIndexMapPtr, _preprocessedLength),
                ParsingResult = new ParsingResult(_parsedNodesPtr, _parsedLength)
            };

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _interpreter.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "4. Planner - Look-Ahead & S-Curve")]
        public unsafe void Layer4_Planner()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                InterpretationResult = new InterpretationResult(_interpretatedFramesPtr, _interpretatedMachineFramesPtr, _interpretatedLength)
            };
            
            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _planner.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "5. Interpolator - Trajectory Blocks")]
        public unsafe void Layer5_Interpolator()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                InterpretationResult = new InterpretationResult(_interpretatedFramesPtr, _interpretatedMachineFramesPtr, _interpretatedLength),
                PlanningResult = new PlanningResult(_plannedBlocksPtr, _plannedLength)
            };

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _interpolator.Process(ref context, ref _runtimeBag);
        }

        [Benchmark(Description = "6. Postprocessor - Binary Serialization")]
        public unsafe void Layer6_Postprocessor()
        {
            VeloxContext<ReadOnlyMemory<char>, byte> context = new(ref _gCodeText, ref _runtimeArena)
            {
                InterpretationResult = new InterpretationResult(_interpretatedFramesPtr, _interpretatedMachineFramesPtr, _interpretatedLength),
                InterpolationResult = new InterpolationResult(_interpolatedPointsPtr, _interpolatedPointsCount, _interpolatedSegmentsPtr, _interpolatedSegmentsCount)
            };

            _runtimeArena.Reset();
            _runtimeBag.Clear();

            _postprocessor.Process(ref context, ref _runtimeBag);
        }
    }
}
