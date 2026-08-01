namespace NG.Velox.Factories
{
    using NG.Velox.Interpolation.Core;
    using NG.Velox.Interpretation.Core;
    using NG.Velox.Lexing.Core;
    using NG.Velox.Parsing.Core;
    using NG.Velox.Pipeline.Core;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Pipeline.Interfaces;
    using NG.Velox.Planning.Core;
    using NG.Velox.Postprocessing.Core;
    using NG.Velox.Postprocessing.Data;
    using NG.Velox.Preprocessing.Core;

    /// <summary>
    /// Factory for creating pre-configured Velox pipelines.
    /// </summary>
    /// <remarks>
    /// Use this factory to quickly instantiate a pipeline with sensible defaults,
    /// or pass custom <see cref="VeloxPipelineOptions"/> to adapt to your specific CNC machine.
    /// </remarks>
    public static class VeloxPipelineFactory
    {
        /// <summary>
        /// Creates a pipeline for trajectory simulation and visualization.
        /// Returns self-contained <see cref="SimulationFrame"/> DTOs suitable for JSON export and Python visualization.
        /// </summary>
        /// <param name="options">
        /// Optional machine-specific options. If null, uses <see cref="VeloxPipelineOptions.Default"/>.
        /// </param>
        /// <returns>A pipeline that outputs simulation frames.</returns>
        /// <example>
        /// <code>
        /// var pipeline = VeloxPipelineFactory.CreateSimulation();
        /// var result = new PipelineResult&lt;SimulationFrame&gt;();
        /// var diag = new DiagnosticBag();
        /// pipeline.Process(gcode.AsMemory(), ref result, ref diag);
        /// </code>
        /// </example>
        public static IVeloxPipeline<ReadOnlyMemory<char>, SimulationFrame> CreateSimulation(
            in VeloxPipelineOptions? options = null)
        {
            var opts = options ?? VeloxPipelineOptions.Default;

            return new VeloxPipeline<ReadOnlyMemory<char>, SimulationFrame>(
                new VeloxMemoryPreprocessor(),
                new VeloxLexer(),
                new VeloxParser(),
                new VeloxInterpreter(),
                new VeloxPlanner(opts),
                new VeloxInterpolator(opts),
                new VeloxSimulationPostprocessor()
            );
        }

        /// <summary>
        /// Creates a pipeline for real-time CNC machine control.
        /// Returns binary data (byte[]) optimized for UART/Ethernet transmission to microcontrollers.
        /// </summary>
        /// <param name="options">
        /// Optional machine-specific options. If null, uses <see cref="VeloxPipelineOptions.Default"/>.
        /// </param>
        /// <returns>A pipeline that outputs binary machine commands.</returns>
        /// <example>
        /// <code>
        /// var pipeline = VeloxPipelineFactory.CreateHardware();
        /// var result = new PipelineResult&lt;byte&gt;();
        /// var diag = new DiagnosticBag();
        /// pipeline.Process(gcode.AsMemory(), ref result, ref diag);
        /// serialPort.Write(result.Values);
        /// </code>
        /// </example>
        public static IVeloxPipeline<ReadOnlyMemory<char>, byte> CreateHardware(
            in VeloxPipelineOptions? options = null)
        {
            var opts = options ?? VeloxPipelineOptions.Default;

            return new VeloxPipeline<ReadOnlyMemory<char>, byte>(
                new VeloxMemoryPreprocessor(),
                new VeloxLexer(),
                new VeloxParser(),
                new VeloxInterpreter(),
                new VeloxPlanner(opts),
                new VeloxInterpolator(opts),
                new VeloxHardwarePostprocessor()
            );
        }
    }
}