using System.Runtime.CompilerServices;

namespace NG.Velox.Parsing.Core
{
    using NG.Velox.Helpers;
    using NG.Velox.Context.Interfaces;
    using NG.Velox.Diagnostic.Core;
    using NG.Velox.Diagnostic.Data;
    using NG.Velox.Lexing.Data;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Parsing.Interfaces;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Converts a flat lexical stream into a strictly packed Abstract Syntax Tree (AST) using raw unmanaged pointer arithmetic.
    /// Collapses empty source lines by emitting <see cref="NodeKind.NewLine"/> markers only at meaningful frame boundaries.
    /// </summary>
    /// <remarks>
    /// <b>Block boundary contract:</b> A <see cref="NodeKind.NewLine"/> node is emitted only when at least one 
    /// structural node has been successfully produced since the previous block boundary. Consecutive line terminations 
    /// without intervening content are silently collapsed, and trailing line endings after the final block are never materialized.
    /// The interpreter therefore always observes a clean alternating sequence: content nodes, then at most one boundary marker.
    /// </remarks>
    internal sealed unsafe class VeloxParser : IVeloxParser
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process<TContext>(ref TContext context, ref DiagnosticBag diagnosticBag)
            where TContext : struct, IParsingContext, allows ref struct
        {
            LexingResult lexing = context.LexingResult;

            int tokensCount = lexing.Length;

            if (tokensCount == 0)
            {
                context.ParsingResult = new ParsingResult(null, 0);
                return;
            }

            PreprocessingResult preprocessing = context.PreprocessingResult;

            char* textPtr = preprocessing.TextPtr;

            Node* nodesBuffer = ArenaAllocator.Allocate<Node>(ref context.Arena, tokensCount);
            int nodeCount = 0;

            var reader = new TokenReader(in lexing);
            bool blockHasContent = false;

            while (!reader.IsEof)
            {
                Token currentToken = reader.ReadToken();

                if (currentToken.Kind == TokenKind.NewLine)
                {
                    if (blockHasContent)
                    {
                        nodesBuffer[nodeCount++] = new Node(
                            NodeKind.NewLine,
                            currentToken.Start,
                            (ushort)currentToken.Length);
                        blockHasContent = false;
                    }
                    continue;
                }

                if (currentToken.Kind == TokenKind.Number)
                {
                    diagnosticBag.Add(new Diagnostic(
                        code: 201,
                        start: currentToken.Start,
                        length: currentToken.Length,
                        severity: Severity.Error
                    ));
                    continue;
                }

                if (currentToken.Kind == TokenKind.Address)
                {
                    char addressChar = textPtr[currentToken.Start];

                    ref readonly Token nextToken = ref reader.Peek();

                    if (Unsafe.IsNullRef(in nextToken) || nextToken.Kind != TokenKind.Number)
                    {
                        diagnosticBag.Add(new Diagnostic(
                            code: 202,
                            start: currentToken.Start,
                            length: currentToken.Length,
                            severity: Severity.Error
                        ));

                        if (TryMapNode(addressChar, currentToken.Start, (ushort)currentToken.Length, 0.0, out Node errorNode))
                        {
                            nodesBuffer[nodeCount++] = errorNode;
                            blockHasContent = true;
                        }
                        continue;
                    }

                    Token numberToken = reader.ReadToken();

                    int totalLength = (numberToken.Start + numberToken.Length) - currentToken.Start;

                    if (ParsingHelper.TryParseValue(textPtr + numberToken.Start, numberToken.Length, out double parsedValue))
                    {
                        if (TryMapNode(addressChar, currentToken.Start, (ushort)totalLength, parsedValue, out Node validNode))
                        {
                            nodesBuffer[nodeCount++] = validNode;
                            blockHasContent = true;
                        }
                        else
                        {
                            diagnosticBag.Add(new Diagnostic(
                                code: 203,
                                start: currentToken.Start,
                                length: currentToken.Length,
                                severity: Severity.Error
                            ));
                        }
                    }
                    else
                    {
                        diagnosticBag.Add(new Diagnostic(
                            code: 204,
                            start: currentToken.Start,
                            length: currentToken.Length,
                            severity: Severity.Error
                        ));
                    }
                }
            }

            context.ParsingResult = new ParsingResult(nodesBuffer, nodeCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryMapNode(char address, int start, ushort length, double value, out Node node)
        {
            CharMask mask = CharRegistry.GetMask(address);

            if ((mask & CharMask.Coordinate) != 0)
            {
                CoordinateKind coordKind = address switch
                {
                    'X' => CoordinateKind.X,
                    'Y' => CoordinateKind.Y,
                    'Z' => CoordinateKind.Z,
                    _ => (CoordinateKind)0
                };

                node = new Node(NodeKind.Coordinate, coordKind, start, length, value);
                return true;
            }

            if ((mask & CharMask.Command) != 0)
            {
                CommandKind cmdKind = address switch
                {
                    'G' => CommandKind.G,
                    'M' => CommandKind.M,
                    _ => (CommandKind)0
                };

                node = new Node(NodeKind.Command, cmdKind, start, length, value);
                return true;
            }

            if ((mask & CharMask.Parameter) != 0)
            {
                ParameterKind paramKind = address switch
                {
                    'F' => ParameterKind.F,
                    'S' => ParameterKind.S,
                    'P' => ParameterKind.P,
                    'I' => ParameterKind.I,
                    'J' => ParameterKind.J,
                    'K' => ParameterKind.K,
                    'R' => ParameterKind.R,
                    _ => (ParameterKind)0
                };

                node = new Node(NodeKind.Parameter, paramKind, start, length, value);
                return true;
            }

            node = default;
            return false;
        }
    }
}
