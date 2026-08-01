namespace NG.Velox.Context.Interfaces
{
    using NG.Velox.Lexing.Data;
    using NG.Velox.Parsing.Data;
    using NG.Velox.Preprocessing.Data;

    /// <summary>
    /// Defines the execution context interface for the structural syntax parsing phase of the pipeline.
    /// </summary>
    /// <remarks>
    /// Exposes unified access to the results of previous pipeline phases alongside the property for publishing 
    /// the syntax results. Enforces clean data boundaries and data locality.
    /// </remarks>
    internal interface IParsingContext : IArenaContext
    {
        /// <summary>
        /// Gets the immutable results produced during the initial text preprocessing phase.
        /// </summary>
        /// <value>
        /// The <see cref="PreprocessingResult"/> structure containing structural metadata.
        /// </value>
        PreprocessingResult PreprocessingResult { get; }

        /// <summary>
        /// Gets the tokens produced during the lexical analysis phase.
        /// </summary>
        /// <value>
        /// The <see cref="LexingResult"/> structure containing raw parsed tokens and positions.
        /// </value>
        LexingResult LexingResult { get; }

        /// <summary>
        /// Sets the structural parsing result containing direct executable commands or AST blocks.
        /// </summary>
        /// <value>
        /// The <see cref="ParsingResult"/> structural container used to store syntax graphs or word parameters.
        /// </value>
        ParsingResult ParsingResult { set; }
    }
}
