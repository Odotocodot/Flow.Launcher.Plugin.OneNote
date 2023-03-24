using System.Collections.Generic;

namespace Odotocodot.OneNote
{
    public record OneNoteNotebookExt : OneNoteNotebook
    {
        /// <summary>
        /// Sections and section groups of the notebook
        /// </summary>
        public IEnumerable<IOneNoteSectionBase> Sections { get; internal set; }
    }
}