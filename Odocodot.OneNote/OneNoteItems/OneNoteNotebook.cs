using System.Drawing;

namespace Odotocodot.OneNote
{
    public record OneNoteNotebook : IOneNoteItem
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
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

    }
}