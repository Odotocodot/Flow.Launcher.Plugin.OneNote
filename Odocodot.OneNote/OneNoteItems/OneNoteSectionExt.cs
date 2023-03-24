using System.Collections.Generic;

namespace Odotocodot.OneNote
{
    public record OneNoteSectionExt : OneNoteSection, IOneNoteSectionBase
    {
        public IEnumerable<OneNotePage> Pages { get; internal set; }
        public IOneNoteItem Parent { get; internal set; }
        public OneNoteNotebook Notebook { get; internal set; }
        public bool IsSectionGroup => false;
    }
}