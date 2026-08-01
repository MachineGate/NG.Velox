using System.Runtime.InteropServices;

namespace NG.Velox.Parsing.Data
{
    /// <summary>
    /// Represents a dense Abstract Syntax Tree (AST) node for an individual G-code structural element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Memory Layout &amp; Union Optimization:</b> This structure enforces an explicit <see cref="LayoutKind.Explicit"/> memory layout 
    /// with a strict total size of 16 bytes. By leveraging value-type overlapping, the sub-category enums 
    /// (<see cref="ParameterKind"/>, <see cref="CommandKind"/>, and <see cref="CoordinateKind"/>) share the exact same memory offset location. 
    /// Only one sub-kind field is semantically valid at any given time, uniquely dictated by the primary discriminator <see cref="Kind"/>. 
    /// This union pattern shrinks the layout from an otherwise 24+ byte unaligned footprint down to a fixed 16-byte structure.
    /// </para>
    /// <para>
    /// <b>Cache Efficiency Impact:</b> Because processing pipelines evaluate millions of active nodes during large-scale toolpath execution, 
    /// saving 8 bytes per record yields massive reductions in overall system memory allocation while dramatically enhancing 
    /// CPU L1/L2 cache locality and sequential blitting speeds.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal readonly struct Node
    {
        /// <summary>
        /// Gets the primary architectural classification flag used as the discriminator for overlapping fields.
        /// </summary>
        [FieldOffset(0)]
        public readonly NodeKind Kind;

        /// <summary>
        /// Gets the auxiliary parameter variant type. Valid only when <see cref="Kind"/> matches parameter contexts.
        /// </summary>
        [FieldOffset(1)]
        public readonly ParameterKind ParameterKind;

        /// <summary>
        /// Gets the specific machine execution instruction code type. Valid only when <see cref="Kind"/> matches command contexts.
        /// </summary>
        [FieldOffset(1)]
        public readonly CommandKind CommandKind;

        /// <summary>
        /// Gets the targeted translation axis configuration type. Valid only when <see cref="Kind"/> matches coordinate contexts.
        /// </summary>
        [FieldOffset(1)]
        public readonly CoordinateKind CoordinateKind;

        /// <summary>
        /// Gets the total string literal span length representing this syntax component in characters.
        /// </summary>
        [FieldOffset(2)]
        public readonly ushort Length;

        /// <summary>
        /// Gets the absolute zero-based character index mapping to the element's start within the source buffer block.
        /// </summary>
        [FieldOffset(4)]
        public readonly int Start;

        /// <summary>
        /// Gets the evaluated double-precision floating-point literal magnitude associated with this element.
        /// </summary>
        [FieldOffset(8)]
        public readonly double Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> structure bound to a coordinate axis specification.
        /// </summary>
        /// <param name="kind">The primary classification discriminator mapping node contexts.</param>
        /// <param name="coordinateKind">The target linear translation axis configuration.</param>
        /// <param name="start">The absolute tracking source text offset index.</param>
        /// <param name="length">The character stream slice block width.</param>
        /// <param name="value">The parsed numerical double-precision numeric scale magnitude.</param>
        public Node(NodeKind kind, CoordinateKind coordinateKind, int start, ushort length, double value)
        {
            Kind = kind;
            CoordinateKind = coordinateKind;
            Start = start;
            Length = length;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> structure bound to a structural CNC instruction code.
        /// </summary>
        /// <param name="kind">The primary classification discriminator mapping node contexts.</param>
        /// <param name="commandKind">The specific targeted instruction group definition.</param>
        /// <param name="start">The absolute tracking source text offset index.</param>
        /// <param name="length">The character stream slice block width.</param>
        /// <param name="value">The parsed numerical double-precision numeric scale magnitude.</param>
        public Node(NodeKind kind, CommandKind commandKind, int start, ushort length, double value)
        {
            Kind = kind;
            CommandKind = commandKind;
            Start = start;
            Length = length;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> structure bound to an auxiliary coordinate or macro modifier.
        /// </summary>
        /// <param name="kind">The primary classification discriminator mapping node contexts.</param>
        /// <param name="parameterKind">The structural offset or geometry helper assignment key identifier.</param>
        /// <param name="start">The absolute tracking source text offset index.</param>
        /// <param name="length">The character stream slice block width.</param>
        /// <param name="value">The parsed numerical double-precision numeric scale magnitude.</param>
        public Node(NodeKind kind, ParameterKind parameterKind, int start, ushort length, double value)
        {
            Kind = kind;
            ParameterKind = parameterKind;
            Start = start;
            Length = length;
            Value = value;
        }
    }
}
