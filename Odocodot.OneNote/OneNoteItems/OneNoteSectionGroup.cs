using System;
using System.Collections.Generic;

namespace Odotocodot.OneNote
{
    public record OneNoteSectionGroup : IOneNoteSectionBase
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }

        public string Path { get; init; }
        public bool IsSectionGroup => true;
        
        public IEnumerable<IOneNoteSectionBase> Sections { get; internal set; }

        public IOneNoteItem Parent { get; init; }
        public OneNoteNotebook Notebook { get; init; }
    }
}
