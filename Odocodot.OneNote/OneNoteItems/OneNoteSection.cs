using System;
using System.Drawing;

namespace Odotocodot.OneNote
{
    public record OneNoteSection
    {
        public string ID { get; init; }
        public string Name { get; init; }
        public bool IsUnread { get; init; }
        public string Path { get; init; }
        public bool Encrypted { get; init; }
        public Color? Color { get; init; }

    }
}