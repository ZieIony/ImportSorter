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
        private readonly List<List<string>> results;

        public SortImportsSuggestedAction(List<List<ITextSnapshotLine>> blocks) {
            this.blocks = blocks;
            results = new List<List<string>>();
            foreach (var block in blocks) {
                var result = new List<string>();
                foreach (var line in block)
                    result.Add(line.GetText());
                result.Sort((t1, t2) => {
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
                results.Add(result);
            }
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken) {
            var textBlock = new TextBlock {
                Padding = new Thickness(5)
            };
            textBlock.Inlines.Add(new Run() {
                Text = string.Join("\n\n", results.Select(result => string.Join("\n", result)))
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
            get { return string.Format("Sort {0} imports", blocks.Sum((lines) => lines.Count)); }
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
            get { return true; }
        }

        public void Invoke(CancellationToken cancellationToken) {
            ITextEdit edit = blocks[0][0].Snapshot.TextBuffer.CreateEdit();
            for (int i = 0; i < blocks.Count; i++) {
                var lines = blocks[i];
                int length = lines[lines.Count - 1].End.Position - lines[0].Start.Position;
                var span = new Microsoft.VisualStudio.Text.Span(lines[0].Start.Position, length);
                edit.Replace(span, string.Join("\n", results[i]));
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
