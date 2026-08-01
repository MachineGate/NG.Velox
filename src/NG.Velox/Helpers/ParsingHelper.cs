using System.Globalization;
using System.Runtime.CompilerServices;

namespace NG.Velox.Helpers
{
    /// <summary>
    /// Provides high-performance utility methods for parsing numeric strings within G-code blocks using raw unmanaged pointers.
    /// </summary>
    internal static unsafe class ParsingHelper
    {
        /// <summary>
        /// Configures the strict floating-point notation constraints allowed in standard G-code specifications.
        /// </summary>
        private const NumberStyles NumberStyle = NumberStyles.AllowDecimalPoint
                                               | NumberStyles.AllowLeadingSign
                                               | NumberStyles.AllowExponent;

        /// <summary>
        /// Attempts to convert the unmanaged character memory block representation of a number to its double-precision floating-point equivalent.
        /// </summary>
        /// <param name="pCurrent">A direct unmanaged pointer to the start of the numeric character sequence in memory.</param>
        /// <param name="length">The exact number of sequential characters to read from the memory pointer.</param>
        /// <param name="value">When this method returns, contains the parsed double-precision value if successful, or <c>0.0</c> if failed.</param>
        /// <returns><see langword="true"/> if the source unmanaged block was parsed successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Uses <see cref="CultureInfo.InvariantCulture"/> to guarantee uniform behavior across different OS regional settings, 
        /// ensuring decimal dots are always used instead of commas. By creating a lightweight, stack-bound span view 
        /// directly from the raw memory address, it completely avoids managed heap allocations or string cloning inside hot parsing paths.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseValue(char* pCurrent, int length, out double value)
        {
            if (pCurrent == null || length <= 0)
            {
                value = 0.0;
                return false;
            }

            ReadOnlySpan<char> sourceSpan = new(pCurrent, length);

            return double.TryParse(sourceSpan, NumberStyle, CultureInfo.InvariantCulture, out value);
        }
    }
}
