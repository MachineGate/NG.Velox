using System.Runtime.CompilerServices;

namespace NG.Velox.Helpers
{
    /// <summary>
    /// Represents a bitmask for classifying characters during G-code lexical analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Explicitly backed by <see cref="ushort"/> to ensure cache locality and minimize memory 
    /// footprint within performance-critical tokenization loops.
    /// </para>
    /// <para>
    /// Designed to support bitwise operations, allowing a single character to satisfy 
    /// multiple classifications simultaneously (e.g., a character can be evaluated as both a digit and part of a coordinate).
    /// </para>
    /// </remarks>
    [Flags]
    internal enum CharMask : ushort
    {
        /// <summary>
        /// The character does not belong to any recognized syntactic category.
        /// </summary>
        None = 0,

        /// <summary>
        /// A letter identifying a coordinate axis (e.g., X, Y, Z, A, B, C).
        /// </summary>
        Coordinate = 1 << 0,

        /// <summary>
        /// A letter identifying a G-code or M-code command identifier (e.g., G, M, T, F, S).
        /// </summary>
        Command = 1 << 1,

        /// <summary>
        /// A letter specifying an auxiliary parameter or sub-command modifier (e.g., P, Q, I, J, K).
        /// </summary>
        Parameter = 1 << 2,

        /// <summary>
        /// A valid numeric digit (0-9) or associated numeric character (e.g., minus sign, decimal point).
        /// </summary>
        Digit = 1 << 3,

        /// <summary>
        /// An exponent character indicating scientific notation for floating-point values (E or e).
        /// </summary>
        Exponent = 1 << 4,

        /// <summary>
        /// An opening bracket character indicating the start of a comment, expression, or subprogram block (e.g., '(' or '[').
        /// </summary>
        OpenBracket = 1 << 5,

        /// <summary>
        /// A closing bracket character indicating the end of a comment, expression, or subprogram block (e.g., ')' or ']').
        /// </summary>
        CloseBracket = 1 << 6,

        /// <summary>
        /// A semicolon character used to initiate an end-of-line comment.
        /// </summary>
        Semicolon = 1 << 7,

        /// <summary>
        /// A horizontal whitespace character used for separating syntax elements (e.g., space or tab).
        /// </summary>
        Whitespace = 1 << 8,

        /// <summary>
        /// A newline or line-ending control character indicating the end of a block or command line (e.g., CR or LF).
        /// </summary>
        NewLine = 1 << 9,

        /// <summary>
        /// A line number label (e.g. NXXX).
        /// </summary>
        Label = 1 << 10,
    }

    /// <summary>
    /// Fast character classification using a lookup table (O(1) per char).
    /// Uses unsafe code for direct memory access - ~10x faster than switch/if-chains.
    /// </summary>
    /// <remarks>
    /// <b>Extension point:</b> To support new address letters (e.g., A/B/C for 5-axis),
    /// add them to the static constructor: <c>Table['A'] = (ushort)CharMask.Coordinate;</c>
    /// </remarks>
    internal static class CharRegistry
    {
        // ASCII-only lookup table (128 bytes). Non-ASCII chars return false in Is().
        private static readonly ushort[] Table = new ushort[128];

        static CharRegistry()
        {
            Table['X'] = Table['Y'] = Table['Z'] = (ushort)CharMask.Coordinate;
            Table['G'] = Table['M'] = (ushort)CharMask.Command;
            Table['F'] = Table['P'] = Table['I'] = Table['J'] = Table['K'] = Table['R'] = (ushort)CharMask.Parameter;
            Table['N'] = (ushort)CharMask.Label;

            for (int i = '0'; i <= '9'; i++)
            {
                Table[i] = (ushort)CharMask.Digit;
            }
            
            Table['.'] = Table['-'] = Table['+'] = (ushort)CharMask.Digit;
            Table['e'] = Table['E'] = (ushort)CharMask.Exponent;

            Table[';'] = (ushort)CharMask.Semicolon;

            Table['('] = (ushort)CharMask.OpenBracket;
            Table[')'] = (ushort)CharMask.CloseBracket;

            Table['\t'] = Table['\r'] = Table[' '] = (ushort)CharMask.Whitespace;

            Table['\n'] = (ushort)CharMask.NewLine;
        }

        /// <summary>
        /// Checks whether an ASCII character matches any of the specified classification flags.
        /// </summary>
        /// <param name="c">The character to evaluate.</param>
        /// <param name="mask">The bitmask of categories to check against.</param>
        /// <returns><see langword="true"/> if the character falls into any of the specified categories; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method relies on a high-speed ASCII lookup table. Any non-ASCII character 
        /// (ordinal value greater than 127) automatically results in <see langword="false"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this char c, CharMask mask)
        {
            // Fast check if symbol is ASCII (0..127).
            if (c > 127) return false;

            // Direct read TablePtr + offset (char code).
            return (Table[c] & (ushort)mask) != 0;
        }

        /// <summary>
        /// Retrieves the complete classification bitmask for a given character.
        /// </summary>
        /// <param name="c">The character to evaluate.</param>
        /// <returns>A <see cref="CharMask"/> containing all matching flags for the character, or <see cref="CharMask.None"/> if the character is non-ASCII.</returns>
        /// <remarks>
        /// Provides O(1) performance by directly indexing into the pre-allocated ASCII lookup table without branching for valid ASCII ranges.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharMask GetMask(this char c)
        {
            return c <= 127 ? (CharMask)Table[c] : CharMask.None;
        }
    }
}
