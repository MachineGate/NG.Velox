using System.Runtime.CompilerServices;

namespace NG.Velox.Context.Data
{
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Interpolation.Data;
    using NG.Velox.Interpretation.Data;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Memory.Core;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Planning.Data;
    using NG.Velox.Postprocessing.Data;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Represents the unified, zero-allocation execution context container that encapsulates 
    /// the entire lifecycle state of the Velox compilation and interpolation pipeline.
    /// </summary>
    /// <typeparam name="TInput">The value type representing the raw, unstructured incoming data payload.</typeparam>
    /// <typeparam name="TOutput">The value type representing the finalized target hardware bytes or standalone simulation entities.</typeparam>
    /// <remarks>
    /// By implementing all pipeline phase contexts within a single <see langword="ref struct"/>, this type 
    /// guarantees absolute data locality on the stack, eliminates heap allocation overhead, and enables 
    /// aggressive JIT-compilation optimization and inlining across explicit interface boundaries.
    /// </remarks>
    internal ref struct VeloxContext<TInput, TOutput> :
        IPreprocessingContext<TInput>,
        ILexingContext,
        IParsingContext,
        IInterpretationContext,
        IPlanningContext,
        IInterpolationContext,
        IPostprocessingContext<TOutput>
        where TInput : notnull
        where TOutput : unmanaged
    {
        private readonly ref TInput _input;

        private readonly ref MemoryArena _arena;

        private PreprocessingResult _preprocessingResult;
        private LexingResult _lexingResult;
        private ParsingResult _parsingResult;
        private InterpretationResult _interpretationResult;
        private PlanningResult _planningResult;
        private InterpolationResult _interpolationResult;
        private PostprocessingResult<TOutput> _postprocessingResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="VeloxContext{TInput, TOutput}"/> structure 
        /// with direct references to the underlying pipeline execution buffers and state records.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VeloxContext(
            ref TInput input,
            ref MemoryArena arena)
        {
            _input = ref input;
            _arena = ref arena;

            _preprocessingResult = default;
            _lexingResult = default;
            _parsingResult = default;
            _interpretationResult = default;
            _planningResult = default;
            _interpolationResult = default;
            _postprocessingResult = default;
        }

        /// <inheritdoc/>
        public readonly ref MemoryArena Arena
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _arena;
        }

        /// <inheritdoc />
        public readonly TInput Input
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _input;
        }

        /// <inheritdoc />
        public PreprocessingResult PreprocessingResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _preprocessingResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _preprocessingResult = value;
        }

        /// <inheritdoc />
        public LexingResult LexingResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _lexingResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lexingResult = value;
        }

        /// <inheritdoc />
        public ParsingResult ParsingResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _parsingResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _parsingResult = value;
        }

        /// <inheritdoc />
        public InterpretationResult InterpretationResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _interpretationResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpretationResult = value;
        }

        /// <inheritdoc />
        public PlanningResult PlanningResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _planningResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _planningResult = value;
        }

        /// <inheritdoc />
        public InterpolationResult InterpolationResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _interpolationResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _interpolationResult = value;
        }

        /// <inheritdoc />
        public PostprocessingResult<TOutput> PostprocessingResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => _postprocessingResult;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _postprocessingResult = value;
        }
    }
}
