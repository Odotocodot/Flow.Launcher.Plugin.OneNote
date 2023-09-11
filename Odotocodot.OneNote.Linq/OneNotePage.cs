using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// Represents a page in OneNote.
    /// </summary>
    public record OneNotePage : IOneNoteItem
    {
        internal OneNotePage() { }
        /// <inheritdoc/>
        public string ID { get; internal set; }
        /// <inheritdoc/>
        public string Name { get; internal set; }
        /// <inheritdoc/>
        public bool IsUnread { get; internal set; }
        /// <inheritdoc/>
        public DateTime LastModified { get; internal set; }
        /// <inheritdoc/>
        public string RelativePath { get; internal set; }
        /// <inheritdoc/>
        public OneNoteNotebook Notebook { get; internal set; }
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Enumerable.Empty<IOneNoteItem>();
        IOneNoteItem IOneNoteItem.Parent => Section;
        /// <summary>
        /// The section that owns this page.
        /// </summary>
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
        /// Indicates whether the page is in the recycle bin.
        /// </summary>
        /// <seealso cref="OneNoteSectionGroup.IsRecycleBin"/>
        /// <seealso cref="OneNoteSection.IsInRecycleBin"/>
        /// <seealso cref="OneNoteSection.IsDeletedPages"/>
        public bool IsInRecycleBin { get; internal set; }
    }
}