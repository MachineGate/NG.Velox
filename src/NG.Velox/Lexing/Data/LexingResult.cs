using System.Runtime.CompilerServices;

namespace NG.Velox.Lexing.Data
{
    /// <summary>
    /// Acts as a high-capacity, zero-allocation immutable container for storing immutable token sequences generated during G-code lexical analysis using raw unmanaged pointers.
    /// </summary>
    /// <remarks>
    /// This is a strictly immutable, stack-only <see langword="ref struct"/> view over contiguous memory blocks 
    /// allocated within a low-level <see cref="MemoryArena"/>. By keeping all generated tokens stored sequentially inside an arena-backed array, 
    /// it ensures maximal CPU cache locality, rapid linear traversal, and optimal parsing throughput. 
    /// <para/>
    /// Because it provides direct raw pointer access, downstream layers like the parser or reader can iterate over tokens 
    /// with zero bounds-checking overhead, completely bypassing the managed heap. Unlike classic pooled or arena collections, 
    /// this structure does not own the underlying memory and does not require explicit finalization via <c>Dispose</c>. 
    /// Its entire lifetime is strictly bound to the stack frame of the execution pipeline.
    /// </remarks>
    internal readonly unsafe ref struct LexingResult
    {
        private readonly Token* _tokens;
        private readonly int _length;

        /// <summary>
        /// Initializes a new immutable instance of the <see cref="LexingResult"/> structure using a direct unmanaged pointer to the generated token block.
        /// </summary>
        /// <param name="tokens">A direct unmanaged pointer to the start of the contiguous token buffer in memory.</param>
        /// <param name="length">The total number of fully evaluated and valid tokens contained within the sequence.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LexingResult(Token* tokens, int length)
        {
            _tokens = tokens;
            _length = length;
        }

        /// <summary>
        /// Gets a raw, direct unmanaged pointer to the start of the contiguous token buffer in memory.
        /// </summary>
        /// <value>A raw pointer of type <see cref="Token"/>* pointing directly to the first element in the arena allocation block.</value>
        public readonly Token* Tokens
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _tokens;
        }

        /// <summary>
        /// Gets the total number of tokens recorded within this lexical analysis result.
        /// </summary>
        /// <value>An integer tracking the exact element count available for linear iteration and parsing.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Gets a high-level read-only view spanning across the recorded syntax token collection.
        /// </summary>
        /// <value>A linear <see cref="ReadOnlySpan{T}"/> mapping exactly to the populated bounds of the arena allocation buffer.</value>
        public readonly ReadOnlySpan<Token> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_tokens, _length);
        }

        /// <summary>
        /// Retrieves a token at the specified zero-based index by a read-only reference (<see langword="ref readonly"/>).
        /// </summary>
        /// <param name="index">The zero-based position of the token within the contiguous sequence buffer.</param>
        /// <returns>A read-only reference to the underlying <see cref="Token"/> structure, avoiding value copying over the stack.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is negative or greater than or equal to <see cref="Length"/>.</exception>
        public readonly ref readonly Token this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowIndexOutOfRange();
                return ref _tokens[index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() =>
            throw new IndexOutOfRangeException("Index was out of range of the LexingResult.");
    }
}
