using System;
using System.Collections.Generic;

namespace Odotocodot.OneNote.Linq
{
    public interface IOneNoteItem
    {
        /// <summary>
        /// ID of the OneNote item.
        /// </summary>
        string ID { get; }
        /// <summary>
        /// Name of the OneNote item.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Whether the item has unread information.
        /// </summary>
        bool IsUnread { get; }
        /// <summary>
        /// The time the item was last modified
        /// </summary>
        DateTime LastModified { get; }
        /// <inheritdoc cref="OneNoteItemType"/>
        OneNoteItemType ItemType { get; }
        /// <summary>
        /// The children of the item, e.g. for a notebook it would be sections and/or section groups. <br/>
        /// If the item has no children an empty <see cref="IEnumerable{T}"/> (where <typeparamref name="T"/> is <see cref="IOneNoteItem"/>) is returned.
        /// </summary>
        IEnumerable<IOneNoteItem> Children { get; }
        IOneNoteItem Parent { get; }
    }
}
