namespace NG.Velox.Helpers
{
    /// <summary>
    /// Provides utility methods for mapping character absolute indices to structural text coordinates.
    /// </summary>
    public static class IndexMapper
    {
        /// <summary>
        /// Converts a 0-based absolute character index into 1-based text coordinates (row and column).
        /// </summary>
        /// <param name="text">The read-only span of character data to analyze.</param>
        /// <param name="index">The 0-based absolute index of the character in the text.</param>
        /// <returns>A tuple containing the 1-based <c>row</c> number and the 1-based <c>column</c> number.</returns>
        public static (int row, int column) GetRowColumn(ReadOnlySpan<char> text, int index)
        {
            if ((uint)index > (uint)text.Length)
                return (1, 1);

            int currentRow = 1;
            int lastLineStartIndex = 0;

            for (int i = 0; i < index; i++)
            {
                if (text[i] == '\n')
                {
                    currentRow++;
                    lastLineStartIndex = i + 1;
                }
            }

            int column = index - lastLineStartIndex + 1;
            return (currentRow, column);
        }
    }
}
