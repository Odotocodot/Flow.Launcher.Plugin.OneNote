using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNotePage : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string RelativePath { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Page;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Enumerable.Empty<IOneNoteItem>();
        /// <summary>
        /// The page level.
        /// </summary>
        public int Level { get; init; }
        /// <summary>
        /// The time when the page was created.
        /// </summary>
        public DateTime Created { get; init; }
        /// <summary>
        /// The time when the page was last modified.
        /// </summary>
        public DateTime LastModified { get; init; }
        /// <summary>
        /// Is the page in the recycle bin.
        /// </summary>
        public bool IsInRecycleBin { get; init; }
    }
}