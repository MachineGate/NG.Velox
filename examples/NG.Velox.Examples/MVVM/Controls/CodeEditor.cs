using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NG.Velox.Examples
{
    /// <summary>
    /// A lightweight <see cref="RichTextBox"/>-derived code editor that binds its text content to a plain
    /// <see cref="string"/> via <see cref="Code"/> and renders diagnostic error spans via <see cref="Errors"/>.
    /// </summary>
    public class CodeEditor : RichTextBox
    {
        private bool _isUpdating;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeEditor"/> class and registers the paragraph style.
        /// </summary>
        public CodeEditor()
        {
            var paragraphStyle = new Style(typeof(Paragraph));
            paragraphStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));
            Resources.Add(typeof(Paragraph), paragraphStyle);
        }

        /// <summary>
        /// Identifies the <see cref="Code"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
            nameof(Code), typeof(string), typeof(CodeEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCodeChanged));

        /// <summary>
        /// Gets or sets the plain-text code content of the editor.
        /// </summary>
        public string Code
        {
            get => (string)GetValue(CodeProperty);
            set => SetValue(CodeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Errors"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorsProperty = DependencyProperty.Register(
            nameof(Errors), typeof(IEnumerable<ErrorInfo>), typeof(CodeEditor),
            new PropertyMetadata(null, OnErrorsChanged));

        /// <summary>
        /// Gets or sets the collection of diagnostic errors to highlight in the editor.
        /// </summary>
        public IEnumerable<ErrorInfo> Errors
        {
            get => (IEnumerable<ErrorInfo>)GetValue(ErrorsProperty);
            set => SetValue(ErrorsProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                Code = ReadDocumentText();
                HighlightErrors();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Reads the full plain text of the document, stripping WPF's implicit trailing paragraph terminator.
        /// </summary>
        private string ReadDocumentText()
        {
            string text = new TextRange(Document.ContentStart, Document.ContentEnd).Text;

            if (text.EndsWith("\r\n"))
                text = text.Substring(0, text.Length - 2);

            return text;
        }

        /// <summary>
        /// Handles changes to the dependency property that stores the raw string code. 
        /// Updates the document view while preserving the user's caret position.
        /// </summary>
        /// <param name="d">The <see cref="DependencyObject"/> (expected to be <see cref="CodeEditor"/>) that changed.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> containing the old and new text value.</param>
        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor || editor._isUpdating) return;

            editor._isUpdating = true;
            try
            {
                int caretOffset = editor.Document.ContentStart.GetOffsetToPosition(editor.CaretPosition);

                editor.Document.Blocks.Clear();
                editor.Document.Blocks.Add(new Paragraph(new Run(e.NewValue as string ?? string.Empty)));

                TextPointer newCaret = editor.Document.ContentStart.GetPositionAtOffset(caretOffset)
                                       ?? editor.Document.ContentEnd;
                editor.CaretPosition = newCaret;
            }
            finally
            {
                editor._isUpdating = false;
            }

            editor.HighlightErrors();
        }

        /// <summary>
        /// Handles changes to the source error collection instance, managing subscriptions 
        /// to <see cref="INotifyCollectionChanged"/> to track dynamic error updates.
        /// </summary>
        /// <param name="d">The <see cref="DependencyObject"/> (expected to be <see cref="CodeEditor"/>) that changed.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> containing the old and new collection instances.</param>
        private static void OnErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor) return;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= editor.OnErrorsCollectionChanged;

            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += editor.OnErrorsCollectionChanged;

            editor.HighlightErrors();
        }

        /// <summary>
        /// Invoked when the elements inside the active error collection change (items added, removed, or cleared). 
        /// Triggers a re-highlighting of the document within a reentrancy-safe block.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> containing the event data.</param>
        private void OnErrorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                HighlightErrors();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Clears any previously applied diagnostic formatting and reapplies red-underlined styling
        /// for every span currently in <see cref="Errors"/>.
        /// </summary>
        /// <remarks>
        /// Always resets formatting first — even when <see cref="Errors"/> is <see langword="null"/> — so that
        /// clearing the error collection reliably removes stale decorations. A single document traversal
        /// resolves all span boundaries; only then are the formatting properties applied.
        /// </remarks>
        private void HighlightErrors()
        {
            ResetFormatting();

            if (Errors is null) return;

            HashSet<int> boundaries = new();
            foreach (var error in Errors)
            {
                if (error.Start < 0 || error.Length <= 0) continue;
                boundaries.Add(error.Start);
                boundaries.Add(error.Start + error.Length);
            }

            if (boundaries.Count == 0) return;

            Dictionary<int, TextPointer?> boundaryPointers = CollectBoundaryPointers(boundaries);

            foreach (var error in Errors)
            {
                if (error.Start < 0 || error.Length <= 0) continue;

                if (!boundaryPointers.TryGetValue(error.Start, out TextPointer? startPointer) || startPointer is null)
                    continue;

                if (!boundaryPointers.TryGetValue(error.Start + error.Length, out TextPointer? endPointer) || endPointer is null)
                    continue;

                TextRange errorRange = new(startPointer, endPointer);
                errorRange.ApplyPropertyValue(TextBlock.ForegroundProperty, Background);
                errorRange.ApplyPropertyValue(TextBlock.BackgroundProperty, Brushes.Red);
                errorRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
            }
        }

        /// <summary>
        /// Restores the default foreground and clears text decorations across the entire document.
        /// </summary>
        private void ResetFormatting()
        {
            TextRange documentRange = new(Document.ContentStart, Document.ContentEnd);
            documentRange.ApplyPropertyValue(TextBlock.ForegroundProperty, Foreground);
            documentRange.ApplyPropertyValue(TextBlock.BackgroundProperty, Background);
            documentRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, null);
        }

        /// <summary>
        /// Walks the document once and resolves every requested character offset to its <see cref="TextPointer"/>.
        /// </summary>
        /// <param name="offsets">The unique character offsets whose positions should be resolved.</param>
        /// <returns>
        /// A dictionary whose values are the resolved pointers, or <see langword="null"/> when the requested
        /// offset lies outside the document's character range. Callers should skip spans whose endpoints are null.
        /// </returns>
        private Dictionary<int, TextPointer?> CollectBoundaryPointers(HashSet<int> offsets)
        {
            Dictionary<int, TextPointer?> result = new(offsets.Count);
            
            foreach (int offset in offsets) result[offset] = null;

            TextPointer pointer = Document.ContentStart;
            TextPointer documentEnd = Document.ContentEnd;
            int position = 0;

            while (pointer is not null && pointer.CompareTo(documentEnd) <= 0)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string runText = pointer.GetTextInRun(LogicalDirection.Forward);
                    int runEnd = position + runText.Length;

                    foreach (int offset in offsets)
                    {
                        if (result[offset] is null && offset >= position && offset <= runEnd)
                        {
                            result[offset] = pointer.GetPositionAtOffset(offset - position);
                        }
                    }

                    position = runEnd;
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }

            return result;
        }
    }
}
