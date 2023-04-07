using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// The OneNote item type, i.e., whether it is a notebook, section group, section or page.
        /// </summary>
        OneNoteItemType ItemType { get; }

        /// <summary>
        /// The children of the item, e.g. for a notebook it would be sections and/or section groups.
        /// </summary>
        /// <returns>
        /// <see cref="Enumerable.Empty{IOneNoteItem}()"/> if the item has no children.
        /// </returns>
        IEnumerable<IOneNoteItem> Children { get; }
    }
}
