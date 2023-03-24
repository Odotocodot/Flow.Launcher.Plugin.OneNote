namespace Odotocodot.OneNote
{
    public record OneNotePageExt : OneNotePage
    {
        public new OneNoteSectionExt Parent { get; init; }
        public new OneNoteNotebookExt Notebook { get; init; }
    }
}