using System.Drawing;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
	public record struct IconGeneratorInfo
	{
		public string Prefix { get; }
		public Color? Color { get; }
		
		public IconGeneratorInfo(IOneNoteItem item)
		{
			switch (item)
			{
				case OneNoteNotebook n:
					Prefix = IconConstants.Notebook;
					Color = n.Color;
					break;
				case OneNoteSectionGroup sg:
					Prefix = sg.IsRecycleBin ? IconConstants.RecycleBin : IconConstants.SectionGroup;
					break;
				case OneNoteSection s:
					Prefix = IconConstants.Section;
					Color = s.Color;
					break;
				case OneNotePage:
					Prefix = IconConstants.Page;
					break;
			}
		}
	}
}