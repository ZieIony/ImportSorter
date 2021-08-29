using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading;

namespace ImportSorter {
    class SortImportsSuggestedActionsSource: ISuggestedActionsSource {
        private const string IMPORT = "import ";

        private readonly ITextView textView;

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public SortImportsSuggestedActionsSource(ITextView textView) {
            this.textView = textView;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken) {
            return Task.Factory.StartNew(() => {
                List<List<ITextSnapshotLine>> blocks = GetImportBlocks();
                return blocks.Count != 0;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken) {
            List<List<ITextSnapshotLine>> blocks = GetImportBlocks();
            if (blocks.Count == 0)
                return null;
            var sortImportsAction = new SortImportsSuggestedAction(blocks);
            return new SuggestedActionSet[] { new SuggestedActionSet(new ISuggestedAction[] { sortImportsAction }) };
        }

        private List<List<ITextSnapshotLine>> GetImportBlocks() {
            SnapshotPoint start = textView.Selection.Start.Position;
            SnapshotPoint end = textView.Selection.End.Position;
            var blocks = new List<List<ITextSnapshotLine>>();
            int lineStart = start.GetContainingLine().LineNumber;
            int lineEnd = end.GetContainingLine().LineNumber;
            if (start == end) {
                ITextSnapshotLine caretLine = start.GetContainingLine();
                if (caretLine.GetText().Trim().StartsWith(IMPORT)) {
                    var lines = new List<ITextSnapshotLine> {
                        caretLine
                    };
                    for (int i = lineStart - 1; i >= 0; i--) {
                        var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                        if (!line.GetText().Trim().StartsWith(IMPORT))
                            break;
                        lines.Insert(0, line);
                    }
                    for (int i = lineEnd + 1; i < textView.TextViewLines.Count; i++) {
                        var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                        if (!line.GetText().Trim().StartsWith(IMPORT))
                            break;
                        lines.Add(line);
                    }
                    if (lines.Count > 1)
                        blocks.Add(lines);
                }
            } else {
                var endMinus1 = end - 1;
                if (lineEnd != endMinus1.GetContainingLine().LineNumber)
                    lineEnd--;
                var lines = new List<ITextSnapshotLine>();
                for (int i = lineStart; i <= lineEnd; i++) {
                    var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                    if (!line.GetText().Trim().StartsWith(IMPORT)) {
                        if (lines.Count > 0) {
                            if (lines.Count > 1)
                                blocks.Add(lines);
                            lines = new List<ITextSnapshotLine>();
                        }
                        continue;
                    }
                    lines.Add(line);
                }
                if (lines.Count > 1)
                    blocks.Add(lines);
            }

            return blocks;
        }

        public void Dispose() {
        }

        public bool TryGetTelemetryId(out Guid telemetryId) {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
