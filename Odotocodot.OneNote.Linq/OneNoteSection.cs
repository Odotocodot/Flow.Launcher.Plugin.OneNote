using System.Collections.Generic;
using System.Drawing;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSection : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string Path { get; init; }
        public string RelativePath { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Section;
        //TODO Implement
        /// <summary>
        /// If the section 
        /// </summary>
        public bool Locked { get; init; }
        /// <summary>
        /// Is the section encrypted.
        /// </summary>
        public bool Encrypted { get; init; }
        /// <summary>
        /// The color of the section.
        /// </summary>
        public Color? Color { get; init; }

        public IEnumerable<OneNotePage> Pages { get; init; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Pages;


    }
}