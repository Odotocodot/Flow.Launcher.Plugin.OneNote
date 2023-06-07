using System;
using System.Collections.Generic;
using System.Drawing;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSection : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string RelativePath { get; init; }
        public DateTime LastModified { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Section;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Pages;
        /// <summary>
        /// Full path of the section.
        /// </summary>
        public string Path { get; init; }
        ///// <summary>
        ///// 
        ///// </summary>
        //public bool IsReadOnly { get; init; }
        /// <summary>
        /// Returns true if an encrypted section has been unlocked allowing access, otherwise false.
        /// </summary>
        public bool Locked { get; init; }
        /// <summary>
        /// Is the section encrypted.
        /// </summary>
        public bool Encrypted { get; init; }
        /// <summary>
        /// Is the section in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; init; }
        /// <summary>
        /// If true this is a special section that contains all the recently deleted pages in this sections notebook.
        /// </summary>
        public bool IsDeletedPages { get; init; }
        /// <summary>
        /// The color of the section.
        /// </summary>
        public Color? Color { get; init; }
        /// <summary>
        /// The collection of pages within this section.
        /// </summary>
        public IEnumerable<OneNotePage> Pages { get; init; }
    }
}