namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// The OneNote item type, i.e., whether it is a notebook, section group, section or page.
    /// </summary>
    public enum OneNoteItemType
    {
        Notebook,
        SectionGroup,
        Section,
        Page,
    }
    /// <summary>
    /// The type of OneNote recycle bin item.
    /// </summary>
    public enum RecycleBinItemType
    {
        /// <summary>
        /// This item is not in/is the recycle bin.
        /// </summary>
        None,
        /// <summary>
        /// A deleted page.
        /// </summary>
        DeletedPage,
        /// <summary>
        /// The section that contains all the deleted pages.
        /// </summary>
        DeletedPages,
        /// <summary>
        /// The section group that contains the "Deleted Pages" section and all the deleted sections.
        /// </summary>
        RecycleBin,
    }
}
