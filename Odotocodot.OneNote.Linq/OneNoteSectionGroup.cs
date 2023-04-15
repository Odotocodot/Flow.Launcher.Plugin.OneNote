using System.Collections.Generic;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSectionGroup : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string RelativePath { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.SectionGroup;

        public string Path { get; init; }
        /// <summary>
        /// Contains the direct children of a section group, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<IOneNoteItem> Sections { get; init; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Sections;
    }
}
