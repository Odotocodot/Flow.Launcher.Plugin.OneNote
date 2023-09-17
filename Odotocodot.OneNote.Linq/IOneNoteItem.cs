using System;
using System.Collections.Generic;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// The base interface of OneNote hierarchy items types. <br/>
    /// (These are <see cref="OneNoteNotebook"/>, <see cref="OneNoteSectionGroup"/>, <see cref="OneNoteSection"/> and <see cref="OneNotePage"/>).
    /// </summary>
    public interface IOneNoteItem
    {
        /// <summary>
        /// The ID of the OneNote hierarchy item.
        /// </summary>
        string ID { get; }
        /// <summary>
        /// The name of the OneNote hierarchy item.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Indicates whether the OneNote hierarchy item has unread information.
        /// </summary>
        bool IsUnread { get; }
        /// <summary>
        /// The time when the OneNote hierarchy item was last modified.
        /// </summary>
        DateTime LastModified { get; }
        /// <summary>
        /// The direct children of the OneNote hierarchy <see cref="IOneNoteItem">item</see>, e.g. for a <see cref="OneNoteNotebook">notebook</see> it would be <see cref="OneNoteSection">sections</see> and/or <see cref="OneNoteSectionGroup">section groups</see>. <br/>
        /// If the <see cref="IOneNoteItem">item</see> has no children an empty <see cref="IEnumerable{T}"/> (where <typeparamref name="T"/> is <see cref="IOneNoteItem"/>) is returned.
        /// </summary>
        IEnumerable<IOneNoteItem> Children { get; }
        /// <summary>
        /// The parent of the OneNote hierarchy item. <br/>
        /// <see langword="null"/> if the OneNote item has no parent i.e. a <see cref="OneNoteNotebook">notebook</see>.
        /// </summary>
        IOneNoteItem Parent { get; }
        /// <summary>
        /// The path of the OneNote hierarchy item relative to and inclusive of its <see cref="Notebook">notebook</see>.
        /// </summary>
        string RelativePath { get; }
        /// <summary>
        /// The <see cref="OneNoteNotebook">notebook</see> that contains this OneNote hierarchy item.
        /// </summary>
        OneNoteNotebook Notebook { get; }
    }
}
