using System.Collections.Generic;
using System.Drawing;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteNotebook : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string RelativePath { get; init; }
        OneNoteItemType IOneNoteItem.ItemType => OneNoteItemType.Notebook;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => Sections;
        /// <summary>
        /// Nickname of the notebook.
        /// </summary>
        public string NickName { get; init; }
        /// <summary>
        /// Full path of the notebook.
        /// </summary>
        public string Path { get; init; }
        /// <summary>
        /// Color of the notebook.
        /// </summary>
        public Color? Color { get; init; }
        /// <summary>
        /// Contains the direct children of a notebook, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<IOneNoteItem> Sections { get; init; }
    }
}