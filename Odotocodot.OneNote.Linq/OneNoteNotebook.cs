using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public record OneNoteNotebook : IOneNoteItem
    {
        internal OneNoteNotebook() { }
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public bool IsUnread { get; internal set; }
        public DateTime LastModified { get; internal set; }
        public string RelativePath => Name;
        IEnumerable<IOneNoteItem> IOneNoteItem.Children => ((IEnumerable<IOneNoteItem>)Sections).Concat(SectionGroups);
        IOneNoteItem IOneNoteItem.Parent => null;
        /// <summary>
        /// Nickname of the notebook.
        /// </summary>
        public string NickName { get; internal set; }
        /// <summary>
        /// Full path of the notebook.
        /// </summary>
        public string Path { get; internal set; }
        /// <summary>
        /// Color of the notebook.
        /// </summary>
        public Color? Color { get; internal set; }
        /// <summary>
        /// Contains the direct children of a notebook, i.e., its sections and section groups.
        /// </summary>
        public IEnumerable<OneNoteSection> Sections { get; internal set; }
        public IEnumerable<OneNoteSectionGroup> SectionGroups { get; internal set; }
    }
}