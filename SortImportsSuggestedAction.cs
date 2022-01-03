using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Linq;

namespace ImportSorter {
    class SortImportsSuggestedAction: ISuggestedAction {
        private readonly List<List<ITextSnapshotLine>> blocks;
        private readonly List<List<string>> sortedBlocks;

        public SortImportsSuggestedAction(List<List<ITextSnapshotLine>> blocks) {
            this.blocks = new List<List<ITextSnapshotLine>>();
            sortedBlocks = new List<List<string>>();
            foreach (var block in blocks) {
                var lines = new List<string>();
                var sorted = new List<string>();
                foreach (var line in block) {
                    lines.Add(line.GetText());
                    sorted.Add(line.GetText());
                }
                sorted.Sort((t1, t2) => {
                    var s1 = t1.Split('.');
                    var s2 = t2.Split('.');
                    for (int i = 0; i < Math.Max(s1.Length, s2.Length); i++) {
                        if (i == s1.Length)
                            return -1;
                        if (i == s1.Length)
                            return 1;
                        var cmp = string.Compare(s1[i], s2[i]);
                        if (cmp != 0)
                            return cmp;
                    }
                    return 0;
                });
                for (int i = 0; i < lines.Count; i++) {
                    if (lines[i] != sorted[i]) {
                        this.blocks.Add(block);
                        sortedBlocks.Add(sorted);
                        break;
                    }
                }
            }
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken) {
            var textBlock = new TextBlock {
                Padding = new Thickness(5)
            };
            textBlock.Inlines.Add(new Run() {
                Text = string.Join("\n\n", sortedBlocks.Select(result => string.Join("\n", result)))
            });
            return Task.FromResult<object>(textBlock);
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public bool HasActionSets {
            get { return false; }
        }

        public string DisplayText {
            get {
                int importsToSort = blocks.Sum((lines) => lines.Count);
                return importsToSort == 0 ? "Imports already sorted" : string.Format("Sort {0} imports", importsToSort);
            }
        }

        public ImageMoniker IconMoniker {
            get { return default; }
        }

        public string IconAutomationText {
            get {
                return null;
            }
        }
        public string InputGestureText {
            get {
                return null;
            }
        }
        public bool HasPreview {
            get { return blocks.Count > 0; }
        }

        public void Invoke(CancellationToken cancellationToken) {
            if (blocks.Count == 0)
                return;
            ITextEdit edit = blocks[0][0].Snapshot.TextBuffer.CreateEdit();
            for (int i = 0; i < blocks.Count; i++) {
                var lines = blocks[i];
                int length = lines[lines.Count - 1].End.Position - lines[0].Start.Position;
                var span = new Microsoft.VisualStudio.Text.Span(lines[0].Start.Position, length);
                edit.Replace(span, string.Join(Environment.NewLine, sortedBlocks[i]));
            }
            edit.Apply();
        }

        public void Dispose() {
        }

        public bool TryGetTelemetryId(out Guid telemetryId) {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
