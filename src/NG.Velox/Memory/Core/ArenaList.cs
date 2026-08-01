using System.Runtime.CompilerServices;

namespace NG.Velox.Memory.Core
{
    using NG.Velox.Helpers;

    /// <summary>
    /// Represents a fast, stack-allocated, resizable list that allocates memory from a <see cref="MemoryArena"/>.
    /// </summary>
    /// <typeparam name="T">The type of unmanaged elements in the list.</typeparam>
    /// <remarks>
    /// This is a <see langword="ref struct"/> and cannot be escaped to the heap. Memory expansion relies on the 
    /// underlying arena allocator, which means old memory blocks remain in the arena until it is wiped entirely.
    /// </remarks>
    internal unsafe ref struct ArenaList<T> where T : unmanaged
    {
        private ref MemoryArena _arena;
        private T* _items;
        private int _capacity;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArenaList{T}"/> struct with a specified initial capacity.
        /// </summary>
        /// <param name="arena">A reference to the <see cref="MemoryArena"/> used for backing store allocations.</param>
        /// <param name="initialCapacity">The initial number of elements the list can store before resizing. Defaults to 4.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArenaList(ref MemoryArena arena, int initialCapacity = 4)
        {
            _arena = ref arena;
            _count = 0;
            _capacity = initialCapacity > 0 ? initialCapacity : 4;

            _items = ArenaAllocator.Allocate<T>(ref _arena, _capacity);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ArenaList{T}"/>.
        /// </summary>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _capacity;
        }

        /// <summary>
        /// Gets a managed reference to the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    ThrowIndexOutOfRangeException();
                }
                return ref _items[index];
            }
        }

        /// <summary>
        /// Adds an item to the end of the <see cref="ArenaList{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the list.</param>
        /// <exception cref="OverflowException">Thrown during expansion if the new capacity exceeds maximum integer boundaries.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_count == _capacity)
            {
                Grow();
            }

            _items[_count] = item;
            _count++;
        }

        /// <summary>
        /// Clears the list by resetting the element count to zero without freeing the underlying arena memory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _count = 0;
        }

        /// <summary>
        /// Doubles the capacity of the list by allocating a new block from the arena and copying existing elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow()
        {
            int newCapacity = _capacity == 0 ? 4 : checked(_capacity * 2);

            T* newItems = ArenaAllocator.Allocate<T>(ref _arena, newCapacity);

            if (_count > 0)
            {
                long bytesToCopy = sizeof(T) * _count;
                Buffer.MemoryCopy(_items, newItems, bytesToCopy, bytesToCopy);
            }

            _items = newItems;
            _capacity = newCapacity;
        }

        /// <summary>
        /// Returns a direct pointer to the underlying unmanaged buffer.
        /// </summary>
        /// <returns>A raw pointer to the first element of the list buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T* AsPointer() => _items;

        /// <summary>
        /// Helper method to throw an <see cref="IndexOutOfRangeException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRangeException()
            => throw new IndexOutOfRangeException("Index was out of range of the ArenaList.");
    }

}
