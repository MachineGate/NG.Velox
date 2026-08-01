using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NG.Velox.Memory.Core
{
    /// <summary>
    /// Represents a high-performance, contiguous unmanaged memory arena that provides fast, low-overhead linear allocation with alignment guarantees.
    /// </summary>
    /// <remarks>
    /// This struct manages native memory allocated via <see cref="NativeMemory.Alloc(nuint)"/>. It does not support 
    /// individual deallocations; instead, the entire memory block can be reclaimed at once using <see cref="Reset"/> 
    /// or completely released via <see cref="Dispose"/>.
    /// </remarks>
    public unsafe struct MemoryArena : IDisposable
    {
        private readonly byte* _memory;
        private readonly int _capacity;
        private int _offset;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryArena"/> struct with the specified total capacity in bytes.
        /// </summary>
        /// <param name="capacity">The total number of bytes to allocate from unmanaged memory. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than or equal to zero.</exception>
        public MemoryArena(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            _capacity = capacity;
            _offset = 0;
            _memory = (byte*)NativeMemory.Alloc((nuint)capacity);
        }

        /// <summary>
        /// Gets the total size of the unmanaged memory block allocated for this arena, in bytes.
        /// </summary>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _capacity;
        }

        /// <summary>
        /// Gets the total number of bytes currently consumed within the arena, including alignment padding.
        /// </summary>
        public readonly int AllocatedBytes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _offset;
        }

        /// <summary>
        /// Allocates a block of aligned unmanaged memory from the arena.
        /// </summary>
        /// <param name="size">The number of bytes to allocate.</param>
        /// <param name="alignment">The byte alignment boundary (must be a power of two, e.g., 1, 2, 4, 8).</param>
        /// <returns>A raw pointer to the start of the aligned allocated memory block.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the arena has already been disposed.</exception>
        /// <exception cref="OutOfMemoryException">Thrown when the requested size, including alignment padding, exceeds the remaining capacity of the arena.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* Allocate(int size, int alignment)
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(MemoryArena));

            byte* currentPtr = _memory + _offset;
            byte* alignedPtr = (byte*)(((ulong)currentPtr + (ulong)alignment - 1) & ~(ulong)(alignment - 1));

            int realAllocatedSize = (int)(alignedPtr - currentPtr) + size;

            if (_offset + realAllocatedSize > _capacity)
            {
                ThrowOutOfMemoryException(size, _capacity - _offset);
            }

            _offset += realAllocatedSize;

            return alignedPtr;
        }

        /// <summary>
        /// Resets the allocation offset to zero, effectively freeing all previously allocated blocks for reuse without releasing the underlying native memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _offset = 0;
        }

        /// <summary>
        /// Releases the unmanaged memory allocated by the <see cref="MemoryArena"/> and invalidates the instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            if (_memory is not null)
            {
                NativeMemory.Free(_memory);
            }
            _disposed = true;
        }

        /// <summary>
        /// Helper method to throw an <see cref="OutOfMemoryException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfMemoryException(int size, int left)
            => throw new OutOfMemoryException($"Not enough memory in arena. Requested {size} bytes, left: {left} bytes.");
    }
}
