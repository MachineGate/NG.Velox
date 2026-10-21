/// <summary>
/// Represents data about a specific code or text error, including its boundaries 
/// and an optional descriptive message.
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorInfo"/> class with a specified range and message.
    /// </summary>
    /// <param name="start">The zero-based starting character index of the error within the text.</param>
    /// <param name="length">The number of characters spanned by the error.</param>
    /// <param name="text">The optional descriptive text or error message.</param>
    public ErrorInfo(int start, int length, string? text)
    {
        Start = start;
        Length = length;
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorInfo"/> class targeting the beginning of the text.
    /// </summary>
    /// <param name="text">The optional descriptive text or error message.</param>
    public ErrorInfo(string? text)
    {
        Start = 0;
        Length = 0;
        Text = text;
    }

    /// <summary>
    /// Gets or sets the zero-based starting character index of the error.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the number of characters spanned by the error.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the optional descriptive text or message detailing the error.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Returns the error message text, or an empty string if no message is available.
    /// </summary>
    /// <returns>A string representation of the current error.</returns>
    public override string ToString()
    {
        return Text ?? string.Empty;
    }
}
