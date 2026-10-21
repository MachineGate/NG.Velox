using System.Runtime.CompilerServices;

namespace NG.Velox.Helpers
{
    /// <summary>
    /// Provides high-performance, zero-allocation utility methods for tokenizing G-code text blocks using raw unmanaged pointer arithmetic.
    /// </summary>
    internal static unsafe class LexingHelper
    {
        /// <summary>
        /// Sequentially invokes all available trivia-skipping helpers until no further progress is made.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipTrivia(ref char* pCurrent, char* pEnd)
        {
            while (pCurrent < pEnd)
            {
                if (SkipWhitespaces(ref pCurrent, pEnd)) continue;
                if (SkipBlockComment(ref pCurrent, pEnd)) continue;
                if (SkipLineComment(ref pCurrent, pEnd)) continue;
                if (SkipLineNumber(ref pCurrent, pEnd)) continue;
                break;
            }
        }

        /// <summary>
        /// Fast-forwards the tracking pointer past any consecutive whitespace characters within the unmanaged character buffer.
        /// </summary>
        /// <param name="pCurrent">A reference to the current parsing pointer address, which will be advanced past all encountered whitespaces.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <returns><see langword="true"/> if at least one whitespace character was consumed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Recognizes spaces (<c>' '</c>), horizontal tabs (<c>'\t'</c>), and carriage returns (<c>'\r'</c>). 
        /// Line feed characters (<c>'\n'</c>) are intentionally NOT treated as whitespace - they are structural 
        /// block delimiters and must be consumed by the lexer to emit <see cref="NG.Velox.Lexing.Data.TokenKind.NewLine"/> tokens.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipWhitespaces(ref char* pCurrent, char* pEnd)
        {
            char* ptr = pCurrent;

            while (ptr < pEnd && (*ptr).Is(CharMask.Whitespace))
            {
                ptr++;
            }

            bool advanced = ptr != pCurrent;
            pCurrent = ptr;
            return advanced;
        }

        /// <summary>
        /// Fast-forwards the tracking pointer past a parenthesized block comment <c>(...)</c>.
        /// </summary>
        /// <param name="pCurrent">A reference to the current parsing pointer address.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <returns><see langword="true"/> if an opening bracket was encountered and the block comment was consumed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The block comment body may span multiple physical lines; internal line feed characters are silently consumed 
        /// as part of the comment and never materialize as structural block boundaries. If the closing bracket is missing 
        /// before the stream boundary, the pointer stops at <paramref name="pEnd"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipBlockComment(ref char* pCurrent, char* pEnd)
        {
            if (pCurrent >= pEnd || !(*pCurrent).Is(CharMask.OpenBracket)) return false;

            char* ptr = pCurrent + 1;
            while (ptr < pEnd && !(*ptr).Is(CharMask.CloseBracket))
            {
                ptr++;
            }

            if (ptr < pEnd) ptr++;

            pCurrent = ptr;
            return true;
        }

        /// <summary>
        /// Fast-forwards the tracking pointer past a line-terminated comment <c>;...</c>.
        /// </summary>
        /// <param name="pCurrent">A reference to the current parsing pointer address.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <returns><see langword="true"/> if a semicolon was encountered and the line comment was consumed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Scanning stops at the first newline character encountered but does not consume it. 
        /// The line feed remains available for the lexer to emit as a structural <see cref="NG.Velox.Lexing.Data.TokenKind.NewLine"/> token.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipLineComment(ref char* pCurrent, char* pEnd)
        {
            if (pCurrent >= pEnd || !(*pCurrent).Is(CharMask.Semicolon)) return false;

            char* ptr = pCurrent + 1;
            while (ptr < pEnd && !(*ptr).Is(CharMask.NewLine))
            {
                ptr++;
            }

            pCurrent = ptr;
            return true;
        }

        /// <summary>
        /// Fast-forwards the tracking pointer past a G-code line number label <c>N&lt;digits&gt;</c>.
        /// </summary>
        /// <param name="pCurrent">A reference to the current parsing pointer address.</param>
        /// <param name="pEnd">The unmanaged memory address marking the absolute boundary limitation of the input character stream.</param>
        /// <returns><see langword="true"/> if a line number label was consumed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Only triggers when an <c>N</c> address letter is immediately followed by a valid numeric character. 
        /// The numeric portion uses the standard digit classification mask (including signs and decimal separators), 
        /// matching the historical tolerance of the preprocessor-based implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipLineNumber(ref char* pCurrent, char* pEnd)
        {
            if (pCurrent + 1 >= pEnd) return false;
            if (!(*pCurrent).Is(CharMask.Label)) return false;
            if (!(*(pCurrent + 1)).Is(CharMask.Digit)) return false;

            char* ptr = pCurrent + 1;
            while (ptr < pEnd && (*ptr).Is(CharMask.Digit))
            {
                ptr++;
            }

            pCurrent = ptr;
            return true;
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
