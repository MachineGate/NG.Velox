using System.Runtime.CompilerServices;

namespace NG.Velox.Helpers
{
    /// <summary>
    /// Provides high-performance, zero-allocation utility methods for tokenizing G-code text blocks using raw unmanaged pointer arithmetic.
    /// </summary>
    internal static unsafe class LexingHelper
    {
        /// <summary>
        /// Fast-forwards the tracking pointer past any consecutive whitespace or newline characters within the unmanaged character buffer.
        /// </summary>
        /// <param name="pCurrent">A reference to the current parsing pointer address, which will be advanced past all encountered whitespaces.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <remarks>
        /// Eliminates all runtime boundary and pinning checks in the hot loop by executing direct pointer increments. 
        /// Recognizes spaces (<c>' '</c>), horizontal tabs (<c>'\t'</c>), carriage returns (<c>'\r'</c>), and line feeds (<c>'\n'</c>).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipWhitespaces(ref char* pCurrent, char* pEnd)
        {
            char* ptr = pCurrent;

            const CharMask whitespaceMask = CharMask.Whitespace | CharMask.NewLine;
            while (ptr < pEnd && (*ptr).Is(whitespaceMask))
            {
                ptr++;
            }

            pCurrent = ptr;
        }

        /// <summary>
        /// Determines whether the character at the specified unmanaged memory address represents a valid G-code address identifier.
        /// </summary>
        /// <param name="pCurrent">The target unmanaged character address to inspect.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <returns><see langword="true"/> if the target character is classified as a coordinate, command, or parameter flag; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Directly evaluates the memory address contents via <see cref="CharRegistry.Is"/>, matching against a combined 
        /// address mask (<see cref="CharMask.Coordinate"/>, <see cref="CharMask.Command"/>, and <see cref="CharMask.Parameter"/>).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadAddress(char* pCurrent, char* pEnd)
        {
            if (pCurrent >= pEnd) return false;

            const CharMask addressMask = CharMask.Coordinate | CharMask.Command | CharMask.Parameter;
            return (*pCurrent).Is(addressMask);
        }

        /// <summary>
        /// Scans the unmanaged character memory buffer to extract consecutive valid characters belonging to a numeric sequence, calculating its token span length.
        /// </summary>
        /// <param name="pCurrent">The origin memory pointer inside the character stream containing the prospective numeric slice.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <param name="length">When this method returns, contains the number of sequential characters matching the numeric pattern.</param>
        /// <returns><see langword="true"/> if at least one character matches the numeric mask constraints; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Iterates through unmanaged memory blocks using direct pointer comparison. Characters are qualified using a combined 
        /// mask of <see cref="CharMask.Digit"/> and <see cref="CharMask.Exponent"/> to support floating-point and scientific notations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadNumber(char* pCurrent, char* pEnd, out int length)
        {
            if (pCurrent >= pEnd)
            {
                length = 0;
                return false;
            }

            char* ptr = pCurrent;
            const CharMask numberMask = CharMask.Digit | CharMask.Exponent;

            while (ptr < pEnd && (*ptr).Is(numberMask))
            {
                ptr++;
            }

            int readLength = (int)(ptr - pCurrent);

            if (readLength > 0)
            {
                length = readLength;
                return true;
            }

            length = 0;
            return false;
        }
    }
}
