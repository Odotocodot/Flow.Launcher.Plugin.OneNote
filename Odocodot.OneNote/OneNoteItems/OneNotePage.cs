using System;

namespace Odotocodot.OneNote
{
    public record OneNotePage : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public int Level { get; init; }
        public DateTime DateTime { get; init; }
        public DateTime LastModified { get; init; }

        public OneNoteSection Parent { get; init; }
        public OneNoteNotebook Notebook { get; init; }
    }
}