using System.Runtime.CompilerServices;

namespace NG.Velox.Pipeline.Extensions
{
    using NG.Velox.Pipeline.Data;

    /// <summary>
    /// External, high-level marshaling layer for v2.0.0 outputs.
    /// Exposes optional allocation-friendly endpoints for downstream managed consumption.
    /// </summary>
    public static class PipelineResultMarshal
    {
        /// <summary>
        /// Materializes the unmanaged results directly into a newly heap-allocated managed array.
        /// Intended for standard non-real-time contexts (e.g., final disk logging or analytical pipelines).
        /// </summary>
        public static TOutput[] ToArray<TOutput>(this in PipelineResult<TOutput> result) where TOutput : unmanaged
        {
            if (result.Length == 0) return Array.Empty<TOutput>();
            return result.Values.ToArray();
        }

        /// <summary>
        /// High-speed copy operations into an existing managed buffer or slice provided by the caller.
        /// Transfers complete data ownership control to the user.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<TOutput>(this in PipelineResult<TOutput> result, Span<TOutput> destination) where TOutput : unmanaged
        {
            result.Values.CopyTo(destination);
        }

        /// <summary>
        /// Alternative extension where the caller provides their own managed buffer block to absorb the data stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CopyToBlock<TOutput>(this in PipelineResult<TOutput> result, TOutput[] buffer, int offset) where TOutput : unmanaged
        {
            if (result.Length == 0) return 0;

            Span<TOutput> targetSpan = buffer.AsSpan(offset, result.Length);
            result.Values.CopyTo(targetSpan);
            return result.Length;
        }
    }
}
