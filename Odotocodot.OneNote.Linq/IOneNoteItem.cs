using System;
using System.Collections.Generic;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// The base interface of the OneNote item types. <br/>
    /// (These are <see cref="OneNoteNotebook"/>, <see cref="OneNoteSectionGroup"/>, <see cref="OneNoteSection"/> and <see cref="OneNotePage"/>).
    /// </summary>
    public interface IOneNoteItem
    {
        /// <summary>
        /// The ID of the OneNote item.
        /// </summary>
        string ID { get; }
        /// <summary>
        /// The name of the OneNote item.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Indicates whether the OneNote item has unread information.
        /// </summary>
        bool IsUnread { get; }
        /// <summary>
        /// The time when the OneNote item was last modified.
        /// </summary>
        DateTime LastModified { get; }
        /// <summary>
        /// The direct children of the OneNote <see cref="IOneNoteItem">item</see>, e.g. for a <see cref="OneNoteNotebook">notebook</see> it would be <see cref="OneNoteSection">sections</see> and/or <see cref="OneNoteSectionGroup">section groups</see>. <br/>
        /// If the <see cref="IOneNoteItem">item</see> has no children an empty <see cref="IEnumerable{T}"/> (where <typeparamref name="T"/> is <see cref="IOneNoteItem"/>) is returned.
        /// </summary>
        IEnumerable<IOneNoteItem> Children { get; }
        /// <summary>
        /// The parent of the OneNote item. <br/>
        /// <see langword="null"/> if the OneNote item has no parent i.e. a <see cref="OneNoteNotebook">notebook</see>.
        /// </summary>
        IOneNoteItem Parent { get; }
        /// <summary>
        /// The path of the OneNote item relative to and inclusive of its <see cref="Notebook">notebook</see>.
        /// </summary>
        string RelativePath { get; }
        /// <summary>
        /// The <see cref="OneNoteNotebook">notebook</see> that contains this OneNote item.
        /// </summary>
        OneNoteNotebook Notebook { get; }
    }
}
