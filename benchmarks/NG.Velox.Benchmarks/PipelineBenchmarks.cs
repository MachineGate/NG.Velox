using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace NG.Velox.Benchmarks
{
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Factories;
    using NG.Velox.Memory.Core;
    using NG.Velox.Pipeline.Interfaces;

    [MemoryDiagnoser, CPUUsageDiagnoser]
    public class PipelineBenchmarks
    {
        private ReadOnlyMemory<char> _gCodeText;
        private IVeloxPipeline<ReadOnlyMemory<char>, byte> _pipeline;

        private MemoryArena _arena;
        private DiagnosticBag _diag;

        [Params(10, 100, 1000, 10000, 100000)]
        public int LineCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _pipeline = VeloxPipelineFactory.CreateHardware();
            _gCodeText = GenerateGCode(LineCount).AsMemory();

            _arena = new MemoryArena(1800 * 1024 * 1024);
            _diag = new DiagnosticBag();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _arena.Dispose();
            _diag.Dispose();
        }

        [Benchmark(Description = "Pipeline - Full Processing")]
        public void Pipeline()
        {
            _arena.Reset();
            _diag.Clear();

            _pipeline.Process(_gCodeText, ref _arena, ref _diag);
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
    }
}
