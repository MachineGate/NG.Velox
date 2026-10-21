using System.Runtime.CompilerServices;

namespace NG.Velox.Preprocessing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Preprocessing.Data;
    using NG.Velox.Preprocessing.Interfaces;

    /// <summary>
    /// Provides an unsafe, high-performance preprocessor that copies characters from <see cref="ReadOnlyMemory{T}"/> 
    /// into an arena-allocated unmanaged memory buffer.
    /// </summary>
    /// <remarks>
    /// This class uses raw pointers and stack/arena allocation strategies for zero-allocation 
    /// and maximum throughput during text preprocessing.
    /// </remarks>
    internal unsafe sealed class VeloxMemoryPreprocessor : IVeloxPreprocessor<ReadOnlyMemory<char>>
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPreprocessingContext<ReadOnlyMemory<char>>, allows ref struct
        {
            ReadOnlySpan<char> span = context.Input.Span;
            int length = span.Length;

            if (length == 0)
            {
                context.PreprocessingResult = new PreprocessingResult(null, 0);
                return;
            }

            char* textBuffer = ArenaAllocator.Allocate<char>(ref context.Arena, length);
            span.CopyTo(new Span<char>(textBuffer, length));

            context.PreprocessingResult = new PreprocessingResult(textBuffer, length);
        }
    }
}
