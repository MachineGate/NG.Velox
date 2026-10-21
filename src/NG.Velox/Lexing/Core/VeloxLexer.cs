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
    /// Tokenizes raw G-code text into Address/Number/NewLine tokens using raw unmanaged pointer arithmetic
    /// while performing inline comment, line-number, and whitespace filtering.
    /// </summary>
    /// <remarks>
    /// <b>Input:</b> Unmodified raw text buffer staged in the arena by the preprocessor.
    /// <b>Output:</b> Strictly immutable sequence of tokens with absolute source positions.
    /// <para/>
    /// <b>Error 101:</b> Unknown symbol (not qualified by <see cref="CharRegistry"/> and not recognized as trivia). 
    /// The reported index is the direct absolute position inside the original source buffer; no index map remapping is required.
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

            Token* tokensBuffer = ArenaAllocator.Allocate<Token>(ref context.Arena, textLength);
            int tokenCount = 0;

            while (pCurrent < pEnd)
            {
                LexingHelper.SkipTrivia(ref pCurrent, pEnd);

                if (pCurrent >= pEnd) break;

                int tokenStart = (int)(pCurrent - textStartPtr);

                if ((*pCurrent).Is(CharMask.NewLine))
                {
                    tokensBuffer[tokenCount] = new Token(TokenKind.NewLine, tokenStart, 1);
                    tokenCount++;
                    pCurrent++;
                    continue;
                }

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

                diagnosticBag.Add(new Diagnostic(
                    code: 101,
                    start: tokenStart,
                    length: 1,
                    severity: Severity.Error
                ));

                pCurrent++;
            }

            context.LexingResult = new LexingResult(tokensBuffer, tokenCount);
        }
    }
}
