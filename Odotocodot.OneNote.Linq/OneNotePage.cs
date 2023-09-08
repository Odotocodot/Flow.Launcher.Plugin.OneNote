using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNotePage : IOneNoteItem
    {
        internal OneNotePage() { }
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public bool IsUnread { get; internal set; }
        public DateTime LastModified { get; internal set; }
        public string RelativePath { get; internal set; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Enumerable.Empty<IOneNoteItem>();
        IOneNoteItem IOneNoteItem.Parent => Section;
        public OneNoteSection Section { get; internal set; }
        /// <summary>
        /// The page level.
        /// </summary>
        public int Level { get; internal set; }
        /// <summary>
        /// The time when the page was created.
        /// </summary>
        public DateTime Created { get; internal set; }
        /// <summary>
        /// Is the page in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; internal set; }
    }
}