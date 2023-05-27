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
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Sections;
        /// <summary>
        /// Full path of the section group.
        /// </summary>
        public string Path { get; init; }
        /// <summary>
        /// If true, this is a special section group which contains all the recently deleted sections as well as the "Deleted Pages" section, which contains all the recently deleted pages.
        /// </summary>
        public bool IsRecycleBin { get; init; }
        /// <summary>
        /// Contains the direct children of a section group, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<IOneNoteItem> Sections { get; init; }
    }
}
