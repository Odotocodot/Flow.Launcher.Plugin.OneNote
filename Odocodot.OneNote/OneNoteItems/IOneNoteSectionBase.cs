namespace Odotocodot.OneNote
{
    public interface IOneNoteSectionBase : IOneNoteItem
    {
        public bool IsSectionGroup { get; }
        public IOneNoteItem Parent { get; }
        public OneNoteNotebook Notebook { get; }
    }
}
