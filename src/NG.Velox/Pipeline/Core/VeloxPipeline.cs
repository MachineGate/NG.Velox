using System.Runtime.CompilerServices;

namespace NG.Velox.Pipeline.Core
{
    using NG.Velox.Context.Data;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Interpolation.Interfaces;
    using NG.Velox.Interpretation.Interfaces;
    using NG.Velox.Lexing.Interfaces;
    using NG.Velox.Parsing.Interfaces;
    using NG.Velox.Pipeline.Data;
    using NG.Velox.Pipeline.Interfaces;
    using NG.Velox.Postprocessing.Data;
    using NG.Velox.Postprocessing.Interfaces;
    using NG.Velox.Planning.Interfaces;
    using NG.Velox.Preprocessing.Interfaces;
    using NG.Velox.Memory.Core;

    /// <summary>
    /// Core v2.0.0 architecture implementation of the manufacturing pipeline.
    /// Orchestrates processing layers exclusively within unmanaged memory boundaries using a single shared context.
    /// </summary>
    /// <typeparam name="TInput">The target source data format type (e.g., text stream, string array, file stream).</typeparam>
    /// <typeparam name="TOutput">The resolved unmanaged output data format type compiled by the postprocessing stage.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>Extensible Generic Design:</b> Abstracting TInput and TOutput enables the 
    /// identical pipeline orchestration loop to generate diverse compile targets, such as a raw byte array stream for hardware controllers 
    /// (via a MachinePostprocessor) or flat unmanaged visualization snapshots (via an unmanaged SimulationPostprocessor).
    /// </para>
    /// <para>
    /// <b>Execution Pipeline Flow:</b> The transformation pipeline processes data monotonically through the following sequential stages:
    /// Preprocessor, then Lexer, then Parser, then Interpreter, then Kinematic Planner, then Path Interpolator, then Target Postprocessor.
    /// Each layer records its output into the shared context and utilizes raw pointer arithmetic inside the provided memory arena.
    /// </para>
    /// <para>
    /// <b>Deterministic Short-Circuit Error Handling:</b> Pipeline execution is guarded by a shared DiagnosticBag. 
    /// If any computational layer registers a fatal error status, the execution pipeline short-circuits instantly and halts subsequent processing. 
    /// <i>Note: Memory reclamation is non-destructive; the lifetime and disposal of the underlying <see cref="MemoryArena"/> remain the 
    /// sole responsibility of the upstream caller.</i>
    /// </para>
    /// </remarks>
    internal sealed unsafe class VeloxPipeline<TInput, TOutput> : IVeloxPipeline<TInput, TOutput>
        where TInput : notnull
        where TOutput : unmanaged
    {
        private readonly IVeloxPreprocessor<TInput> _preprocessor;
        private readonly IVeloxLexer _lexer;
        private readonly IVeloxParser _parser;
        private readonly IVeloxInterpreter _interpreter;
        private readonly IVeloxPlanner _planner;
        private readonly IVeloxInterpolator _interpolator;
        private readonly IVeloxPostprocessor<TOutput> _postprocessor;

        internal VeloxPipeline(
            IVeloxPreprocessor<TInput> preprocessor,
            IVeloxLexer lexer,
            IVeloxParser parser,
            IVeloxInterpreter interpreter,
            IVeloxPlanner planner,
            IVeloxInterpolator interpolator,
            IVeloxPostprocessor<TOutput> postprocessor)
        {
            _preprocessor = preprocessor;
            _lexer = lexer;
            _parser = parser;
            _interpreter = interpreter;
            _planner = planner;
            _interpolator = interpolator;
            _postprocessor = postprocessor;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PipelineResult<TOutput> Process(TInput input, ref MemoryArena arena, ref DiagnosticBag diagnosticBag)
        {
            VeloxContext<TInput, TOutput> context = new(ref input, ref arena);

            _preprocessor.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _lexer.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _parser.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _interpreter.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _planner.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _interpolator.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            _postprocessor.Process(ref context, ref diagnosticBag);
            if (diagnosticBag.HasErrors) return default;

            PostprocessingResult<TOutput> result = context.PostprocessingResult;
            return new PipelineResult<TOutput>(result.BufferPtr, result.Length);
        }
    }
}
