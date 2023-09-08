using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSectionGroup : IOneNoteItem
    {
        internal OneNoteSectionGroup() { }
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public bool IsUnread { get; internal set; }
        public DateTime LastModified { get; internal set; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => ((IEnumerable<IOneNoteItem>)Sections).Concat(SectionGroups);
        public IOneNoteItem Parent { get; internal set; }
        public string RelativePath { get; internal set; }
        /// <summary>
        /// Full path of the section group.
        /// </summary>
        public string Path { get; internal set; }
        /// <summary>
        /// If true, this is a special section group which contains all the recently deleted sections as well as the "Deleted Pages" section, which contains all the recently deleted pages.
        /// </summary>
        public bool IsRecycleBin { get; internal set; }
        /// <summary>
        /// Contains the direct children of a section group, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<OneNoteSection> Sections { get; internal set; }
        public IEnumerable<OneNoteSectionGroup> SectionGroups { get; internal set; }
    }
}
