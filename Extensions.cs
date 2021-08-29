using Microsoft.VisualStudio.Text;

namespace ImportSorter {
    static class Extensions {
        public static ITextDocument GetTextDocument(this ITextBuffer TextBuffer) {
            var rc = TextBuffer.Properties.TryGetProperty(
              typeof(ITextDocument), out ITextDocument textDoc);
            return rc == true ? textDoc : null;
        }
    }
}
