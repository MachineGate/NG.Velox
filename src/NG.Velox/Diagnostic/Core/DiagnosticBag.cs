using System.Buffers;
using System.Runtime.CompilerServices;

namespace NG.Velox.Diagnostic.Core
{
    using NG.Velox.Diagnostic.Data;

    /// <summary>
    /// Container for errors and warnings generated during G-code processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="HasErrors"/> to check if processing should be aborted.
    /// Access individual diagnostics via <see cref="Diagnostics"/> span.
    /// </para>
    /// <para>
    /// This struct must be disposed after use to return rented arrays to the pool.
    /// </para>
    /// <b>Usage pattern:</b>
    /// <code>
    /// var bag = new DiagnosticBag();
    /// try { /* processing */ } finally { bag.Dispose(); }
    /// </code>
    /// <b>Error codes convention:</b>
    /// 1xx - Lexer errors (e.g., 101 = unknown symbol)
    /// 2xx - Parser errors (e.g., 202 = missing number after address)
    /// 3xx - Interpreter errors (e.g., 301 = modal group conflict, 302 = arc without params)
    /// </remarks>
    public struct DiagnosticBag : IDisposable
    {
        private const int INITIAL_CAPACITY = 256;

        private int _errorsCount;
        private int _warningsCount;

        private Diagnostic[]? _buffer;
        private int _totalCount;
        private bool _disposed;

        /// <summary>
        /// Gets a value indicating whether any errors were recorded.
        /// </summary>
        public readonly bool HasErrors => _errorsCount > 0;

        /// <summary>
        /// Gets a value indicating whether any warnings were recorded.
        /// </summary>
        public readonly bool HasWarnings => _warningsCount > 0;

        /// <summary>
        /// Gets the diagnostics as a read-only span.
        /// </summary>
        public readonly ReadOnlySpan<Diagnostic> Diagnostics => _buffer is null
            ? ReadOnlySpan<Diagnostic>.Empty
            : _buffer.AsSpan(0, _totalCount);

        /// <summary>
        /// Ensures the specified size of diagnostics bag.
        /// </summary>
        /// <param name="capacity">The requested bag capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity)
        {
            if (_buffer is null) Init(capacity);
            else if (_buffer.Length < capacity) Resize(capacity);
        }

        /// <summary>
        /// Adds a diagnostic (error or warning) to the bag.
        /// </summary>
        /// <param name="diagnostic">The diagnostic to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in Diagnostic diagnostic)
        {
            if (_buffer is null) Init(INITIAL_CAPACITY);
            else if (_totalCount >= _buffer.Length) Resize(_totalCount * 2);

            if (diagnostic.Severity == Severity.Error) _errorsCount++;
            else if (diagnostic.Severity == Severity.Warning) _warningsCount++;

            _buffer![_totalCount++] = diagnostic;
        }

        /// <summary>
        /// Clears all diagnostics from the bag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _totalCount = 0;
            _errorsCount = 0;
            _warningsCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Init(int capacity) => _buffer = ArrayPool<Diagnostic>.Shared.Rent(capacity);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int capacity)
        {
            var oldBuffer = _buffer!;
            var newBuffer = ArrayPool<Diagnostic>.Shared.Rent(capacity);

            oldBuffer.AsSpan(0, _totalCount).CopyTo(newBuffer);
            ArrayPool<Diagnostic>.Shared.Return(oldBuffer, clearArray: false);

            _buffer = newBuffer;
        }

        /// <summary>
        /// Disposes the bag and returns rented arrays to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            if (_buffer is not null)
            {
                ArrayPool<Diagnostic>.Shared.Return(_buffer, clearArray: false);
                _buffer = null;
            }

            _disposed = true;
        }
    }
}
