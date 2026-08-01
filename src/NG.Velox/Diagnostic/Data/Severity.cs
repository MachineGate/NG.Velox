namespace NG.Velox.Diagnostic.Data
{
    /// <summary>
    /// Defines the importance or criticality level of a diagnostic message or system event.
    /// </summary>
    /// <remarks>
    /// Underlying type is explicitly set to <see cref="byte"/> to minimize memory footprint 
    /// during high-throughput logging or binary serialization.
    /// </remarks>
    public enum Severity : byte
    {
        /// <summary>
        /// Informational message. Used for routine tracking, operational milestones, 
        /// or standard state changes that do not require action.
        /// </summary>
        Info,

        /// <summary>
        /// Warning message. Indicates abnormal behavior, potential misconfigurations, 
        /// or recoverable issues that might lead to a critical failure if ignored.
        /// </summary>
        Warning,

        /// <summary>
        /// Error message. Represents unrecoverable failures, invalid user operations, 
        /// or severe hardware anomalies that halt execution or cause data loss.
        /// </summary>
        Error
    }
}
