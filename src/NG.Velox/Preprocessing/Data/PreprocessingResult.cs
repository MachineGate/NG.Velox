using System.Runtime.CompilerServices;

namespace NG.Velox.Preprocessing.Data
{
    /// <summary>
    /// Acts as a high-capacity, zero-allocation immutable container for the raw input character sequence 
    /// staged inside unmanaged memory for downstream pipeline consumption.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over a contiguous unmanaged character block 
    /// allocated within a low-level <see cref="MemoryArena"/>. The buffer holds the unmodified source text; all structural 
    /// filtering, comment removal, line-number elision, and block boundary detection are performed by the lexer during scanning, 
    /// so character offsets remain identical to the original source indices and no auxiliary index mapping is required.
    /// <para/>
    /// This type is a pure format adapter: the preprocessor only copies bytes, the lexer interprets them.
    /// </remarks>
    internal readonly unsafe ref struct PreprocessingResult
    {
        private readonly char* _text;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="PreprocessingResult"/> structure using a raw arena pointer.
        /// </summary>
        /// <param name="text">A direct unmanaged pointer to the start of the contiguous character buffer.</param>
        /// <param name="length">The total number of valid characters available in the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PreprocessingResult(char* text, int length)
        {
            _text = text;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the character text buffer.
        /// </summary>
        /// <value>A raw pointer of type <see cref="char"/>* pointing directly to the first character slot in the arena.</value>
        public readonly char* TextPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _text;
        }

        /// <summary>
        /// Gets the total number of characters recorded in this sequence.
        /// </summary>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the character text buffer.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the character buffer.</value>
        public readonly ReadOnlySpan<char> Text
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_text, _length);
        }
    }
}