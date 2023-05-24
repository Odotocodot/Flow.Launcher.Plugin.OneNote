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
        /// 
        /// </summary>
        public int Level { get; init; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime DateTime { get; init; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime LastModified { get; init; }
        /// <summary>
        /// Returns true if the page is currently in the recycle bin.
        /// </summary>
        public bool IsDeletePage { get; init; }
    }
}