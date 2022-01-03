using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading;
using System.Text.RegularExpressions;

namespace ImportSorter {
    class SortImportsSuggestedActionsSource: ISuggestedActionsSource {
        private const string IMPORT_PATTERN = @"(?<prefix>[ \t]*(?:export )?[ \t]*import[ \t]+)[a-zA-Z0-9\._\:]+?;";
        private const string PREFIX_GROUP = "prefix";

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
                Match match = Regex.Match(caretLine.GetText().Trim(), IMPORT_PATTERN);
                if (match.Success) {
                    string matchPrefix = match.Groups[PREFIX_GROUP].Value;
                    var lines = new List<ITextSnapshotLine> {
                        caretLine
                    };
                    // match lines up
                    for (int i = lineStart - 1; i >= 0; i--) {
                        var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                        Match nextMatch = Regex.Match(line.GetText().Trim(), IMPORT_PATTERN);
                        if (!nextMatch.Success || nextMatch.Groups[PREFIX_GROUP].Value != matchPrefix)
                            break;
                        lines.Insert(0, line);
                    }
                    // match lines down
                    for (int i = lineEnd + 1; i < textView.TextViewLines.Count; i++) {
                        var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                        Match nextMatch = Regex.Match(line.GetText().Trim(), IMPORT_PATTERN);
                        if (!nextMatch.Success || nextMatch.Groups[PREFIX_GROUP].Value != matchPrefix)
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
                string matchPrefix = null;
                for (int i = lineStart; i <= lineEnd; i++) {
                    var line = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(i);
                    Match nextMatch = Regex.Match(line.GetText().Trim(), IMPORT_PATTERN);
                    string nextMatchPrefix = nextMatch.Groups[PREFIX_GROUP].Value;
                    if (nextMatch.Success && (matchPrefix == null || nextMatchPrefix == matchPrefix)) {
                        matchPrefix = nextMatchPrefix;
                        lines.Add(line);
                    } else {
                        matchPrefix = null;
                        // sort only blocks larger than one line
                        if (lines.Count > 1)
                            blocks.Add(lines);
                        if (lines.Count > 0)
                            lines = new List<ITextSnapshotLine>();
                    }
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
