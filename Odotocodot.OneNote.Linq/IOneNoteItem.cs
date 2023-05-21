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
        /// The path of the item relative to and inclusive of its notebook 
        /// </summary>
        string RelativePath { get; }
        /// <inheritdoc cref="OneNoteItemType"/>
        OneNoteItemType ItemType { get; }
        ///// <inheritdoc cref="RecycleBinItemType"/>
        //RecycleBinItemType RecycleBinType { get; }
        /// <summary>
        /// The children of the item, e.g. for a notebook it would be sections and/or section groups.
        /// </summary>
        /// <returns>
        /// An empty <see cref="IEnumerable{T}"/> (where <typeparamref name="T"/> is <see cref="IOneNoteItem"/>), if the item has no children.
        /// </returns>
        IEnumerable<IOneNoteItem> Children { get; }
    }
}
