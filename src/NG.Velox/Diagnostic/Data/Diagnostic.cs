namespace NG.Velox.Diagnostic.Data
{
    /// <summary>
    /// Diagnostic representation.
    /// </summary>
    public readonly struct Diagnostic
    {
        public readonly int Code;
        public readonly int Start;
        public readonly int Length;
        public readonly Severity Severity;

        /// <summary>
        /// Creates a new diagnostic.
        /// </summary>
        /// <param name="code">Message code.</param>
        /// <param name="start">Index in text.</param>
        /// <param name="length">Length of corrupted text.</param>
        /// <param name="severity">Message severity.</param>
        public Diagnostic(int code, int start, int length, Severity severity)
        {
            Code = code;
            Start = start;
            Length = length;
            Severity = severity;
        }
    }
}
