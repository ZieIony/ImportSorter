using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ImportSorter {
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Test Suggested Actions")]
    [ContentType("text")]
    class SortImportsSuggestedActionsSourceProvider: ISuggestedActionsSourceProvider {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer) {
            if (textBuffer == null || textView == null)
                return null;
            var textDocument = textView.TextBuffer.GetTextDocument();
            if (textDocument == null)
                return null;
            if (!textDocument.FilePath.EndsWith("ixx") && !textDocument.FilePath.EndsWith("cpp"))
                return null;
            return new SortImportsSuggestedActionsSource(textView);
        }
    }
}
