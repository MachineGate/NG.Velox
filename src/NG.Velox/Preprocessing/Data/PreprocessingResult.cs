using System.Runtime.CompilerServices;

namespace NG.Velox.Preprocessing.Data
{
    /// <summary>
    /// Acts as a high-capacity, zero-allocation immutable container for storing filtered character sequences 
    /// and their source text index mappings generated during the preprocessing phase.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. It keeps the clean preprocessed text tightly aligned 
    /// with a source offset map to ensure maximal CPU cache locality and rapid linear traversal for downstream layers.
    /// <para/>
    /// Because it provides direct raw pointer access, downstream components like the lexer can iterate over characters 
    /// with zero bounds-checking overhead, while referencing the index map to report accurate diagnostic positions 
    /// relative to the original unparsed source code.
    /// </remarks>
    internal readonly unsafe ref struct PreprocessingResult
    {
        private readonly char* _text;
        private readonly int* _indexMap;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="PreprocessingResult"/> structure using raw arena pointers.
        /// </summary>
        /// <param name="text">A direct unmanaged pointer to the start of the contiguous preprocessed character buffer.</param>
        /// <param name="indexMap">A direct unmanaged pointer to the start of the parallel original source index mapping buffer.</param>
        /// <param name="length">The total number of valid preprocessed characters available in the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PreprocessingResult(char* text, int* indexMap, int length)
        {
            _text = text;
            _indexMap = indexMap;
            _length = length;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the start of the filtered, preprocessed character text buffer.
        /// </summary>
        /// <value>A raw pointer of type <see cref="char"/>* pointing directly to the first character slot in the arena.</value>
        public readonly char* TextPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _text;
        }

        /// <summary>
        /// Gets a direct unmanaged pointer to the parallel index tracking offset entries.
        /// </summary>
        /// <value>A raw pointer of type <see cref="int"/>* pointing directly to the first source index mapping slot in the arena.</value>
        public readonly int* IndexMapPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _indexMap;
        }

        /// <summary>
        /// Gets the total number of characters and index pointers recorded in this preprocessed sequence.
        /// </summary>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the filtered, preprocessed character text buffer.
        /// </summary>
        /// <value>A linear ReadOnlySpan mapping exactly to the populated bounds of the character buffer.</value>
        public readonly ReadOnlySpan<char> Text
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_text, _length);
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the parallel index tracking offset entries.
        /// </summary>
        /// <value>A linear ReadOnlySpan mapping exactly to the populated bounds of the integer source map buffer.</value>
        public readonly ReadOnlySpan<int> IndexMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_indexMap, _length);
        }
    }
}
