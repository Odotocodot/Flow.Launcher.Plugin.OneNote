using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// Represents a notebook in OneNote.
    /// </summary>
    public record OneNoteNotebook : IOneNoteItem
    {
        internal OneNoteNotebook() { }
        /// <inheritdoc/>
        public string ID { get; internal set; }
        /// <inheritdoc/>
        public string Name { get; internal set; }
        /// <inheritdoc/>
        public bool IsUnread { get; internal set; }
        /// <inheritdoc/>
        public DateTime LastModified { get; internal set; }
        /// <inheritdoc/>
        /// <remarks>For a notebook the relative path is equal to its <see cref="Name"/></remarks>
        public string RelativePath => Name;
        /// <inheritdoc/>
        public OneNoteNotebook Notebook { get; internal set; }
        /// <summary>
        /// The direct children of this notebook. <br/>
        /// Equivalent to concatenating <see cref="SectionGroups"/> and <see cref="Sections"/>.
        /// </summary>
        public IEnumerable<IOneNoteItem> Children => ((IEnumerable<IOneNoteItem>)Sections).Concat(SectionGroups);
        IOneNoteItem IOneNoteItem.Parent => null;
        /// <summary>
        /// The nickname of the notebook.
        /// </summary>
        public string NickName { get; internal set; }
        /// <summary>
        /// The full path to the notebook.
        /// </summary>
        public string Path { get; internal set; }
        /// <summary>
        /// The color of the notebook.
        /// </summary>
        public Color? Color { get; internal set; }
        /// <summary>
        /// The sections that this section group contains (direct children only). 
        /// </summary>
        public IEnumerable<OneNoteSection> Sections { get; internal set; }
        /// <summary>
        /// The section groups that this section group contains (direct children only).
        /// </summary>
        public IEnumerable<OneNoteSectionGroup> SectionGroups { get; internal set; }
    }
}