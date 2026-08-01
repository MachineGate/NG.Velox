using System.Runtime.CompilerServices;

namespace NG.Velox.Lexing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Diagnostic.Data;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Lexing.Interfaces;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Tokenizes preprocessed G-code text into Address/Number tokens using raw unmanaged pointer arithmetic without collection wrappers.
    /// Uses <see cref="CharRegistry"/> for O(1) character classification and maximizes CPU L1i/L2 cache efficiency.
    /// </summary>
    /// <remarks>
    /// <b>Input:</b> Immutable preprocessed text buffer (no comments, no N-codes, minimal whitespace).
    /// <b>Output:</b> Strictly immutable sequence of tokens with absolute start positions and lengths.
    /// <para/>
    /// <b>Error 101:</b> Unknown symbol (not qualified by CharRegistry). Reported with its original index 
    /// mapped back via the unmanaged source index map buffer.
    /// </remarks>
    internal sealed unsafe class VeloxLexer : IVeloxLexer
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, ILexingContext, allows ref struct
        {
            PreprocessingResult preprocessing = context.PreprocessingResult;
            
            int textLength = preprocessing.Length;

            if (textLength == 0)
            {
                context.LexingResult = new LexingResult(null, 0);
                return;
            }

            char* textStartPtr = preprocessing.TextPtr;
            char* pCurrent = textStartPtr;
            char* pEnd = textStartPtr + textLength;

            int* indexMapPtr = preprocessing.IndexMapPtr;

            Token* tokensBuffer = ArenaAllocator.Allocate<Token>(ref context.Arena, textLength);
            int tokenCount = 0;

            while (pCurrent < pEnd)
            {
                LexingHelper.SkipWhitespaces(ref pCurrent, pEnd);

                if (pCurrent >= pEnd) break;

                int tokenStart = (int)(pCurrent - textStartPtr);

                if (LexingHelper.TryReadAddress(pCurrent, pEnd))
                {
                    tokensBuffer[tokenCount] = new Token(TokenKind.Address, tokenStart, 1);
                    tokenCount++;
                    pCurrent += 1;
                    continue;
                }

                if (LexingHelper.TryReadNumber(pCurrent, pEnd, out int numLength))
                {
                    tokensBuffer[tokenCount] = new Token(TokenKind.Number, tokenStart, numLength);
                    tokenCount++;
                    pCurrent += numLength;
                    continue;
                }

                int originalIndex = indexMapPtr[tokenStart];

                diagnosticBag.Add(new Diagnostic(
                    code: 101,
                    start: originalIndex,
                    length: 1,
                    severity: Severity.Error
                ));

                pCurrent++;
            }

            context.LexingResult = new LexingResult(tokensBuffer, tokenCount);
        }
    }
}
