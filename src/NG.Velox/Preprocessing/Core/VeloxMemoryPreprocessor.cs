using System.Runtime.CompilerServices;

namespace NG.Velox.Preprocessing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Preprocessing.Data;
    using NG.Velox.Preprocessing.Interfaces;

    /// <summary>
    /// Removes comments, N-codes, and excess whitespace from G-code text.
    /// Builds IndexMap for accurate error reporting.
    /// </summary>
    /// <remarks>
    /// <b>Removal rules:</b>
    /// <list type="bullet">
    /// <item><c>(...)</c> - parenthesized comments (any content, including newlines)</item>
    /// <item><c>;...</c> - line comments (until newline)</item>
    /// <item><c>N123</c> - line numbers (N followed by digits)</item>
    /// <item>Spaces, tabs, <c>\r</c> - whitespace</item>
    /// </list>
    /// <para><b>CRITICAL:</b> <c>\n</c> is PRESERVED (not removed). Interpreter uses it
    /// to count lines and split blocks. If you remove \n, block separation breaks.</para>
    /// <para><b>IndexMap:</b> For each clean char at index <c>i</c>, <c>IndexMap[i]</c> = original index.
    /// Used by SourceMapper to translate error positions back to user's file.</para>
    /// </remarks>
    internal unsafe sealed class VeloxMemoryPreprocessor : IVeloxPreprocessor<ReadOnlyMemory<char>>
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IPreprocessingContext<ReadOnlyMemory<char>>, allows ref struct
        {
            ReadOnlySpan<char> span = context.Input.Span;
            int length = span.Length;

            if (length == 0)
            {
                context.PreprocessingResult = new PreprocessingResult(null, null, 0);
                return;
            }

            char* textBuffer = ArenaAllocator.Allocate<char>(ref context.Arena, length);
            int* indexMapBuffer = ArenaAllocator.Allocate<int>(ref context.Arena, length);

            int i = 0;
            int writeOffset = 0;

            fixed (char* srcPtr = span)
            {
                while (i < length)
                {
                    char c = srcPtr[i];

                    if (c.Is(CharMask.OpenBracket))
                    {
                        i++;
                        while (i < length && !srcPtr[i].Is(CharMask.CloseBracket)) i++;
                        if (i < length) i++;
                        continue;
                    }

                    if (c.Is(CharMask.Semicolon))
                    {
                        i++;
                        while (i < length && !srcPtr[i].Is(CharMask.NewLine)) i++;
                        continue;
                    }

                    if (c.Is(CharMask.Label) && i + 1 < length && srcPtr[i + 1].Is(CharMask.Digit))
                    {
                        i++;
                        while (i < length && srcPtr[i].Is(CharMask.Digit)) i++;
                        continue;
                    }

                    if (c.Is(CharMask.Whitespace))
                    {
                        i++;
                        continue;
                    }

                    textBuffer[writeOffset] = c;
                    indexMapBuffer[writeOffset] = i;

                    writeOffset++;
                    i++;
                }
            }

            context.PreprocessingResult = new PreprocessingResult(textBuffer, indexMapBuffer, writeOffset);
        }
    }
}