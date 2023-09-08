using System;
using System.Collections.Generic;
using System.Drawing;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteSection : IOneNoteItem
    {
        internal OneNoteSection() { }
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public bool IsUnread { get; internal set; }
        public DateTime LastModified { get; internal set; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Pages;
        public IOneNoteItem Parent { get; internal set; }
        public string RelativePath { get; internal set; }
        /// <summary>
        /// Full path of the section.
        /// </summary>
        public string Path { get; internal set; }
        ///// <summary>
        ///// 
        ///// </summary>
        //public bool IsReadOnly { get; internal set; }
        /// <summary>
        /// Returns true if an encrypted section has been unlocked allowing access, otherwise false.
        /// </summary>
        public bool Locked { get; internal set; }
        /// <summary>
        /// Is the section encrypted.
        /// </summary>
        public bool Encrypted { get; internal set; }
        /// <summary>
        /// Is the section in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; internal set; }
        /// <summary>
        /// If true this is a special section that contains all the recently deleted pages in this sections notebook.
        /// </summary>
        public bool IsDeletedPages { get; internal set; }
        /// <summary>
        /// The color of the section.
        /// </summary>
        public Color? Color { get; internal set; }
        /// <summary>
        /// The collection of pages within this section, the same as <see cref="IOneNoteItem.Children"/> for a section.
        /// </summary>
        public IEnumerable<OneNotePage> Pages { get; internal set; }
    }
}