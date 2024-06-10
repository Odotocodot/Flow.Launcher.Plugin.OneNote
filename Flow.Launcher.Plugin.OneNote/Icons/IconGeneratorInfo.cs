using System.Drawing;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
	public record struct IconGeneratorInfo
	{
		public string Prefix { get; }
		public Color? Color { get; }
		
		public IconGeneratorInfo(OneNoteNotebook notebook)
		{
			Prefix = IconConstants.Notebook;
			Color = notebook.Color;
		}
		public IconGeneratorInfo(OneNoteSectionGroup sectionGroup)
		{
			Prefix = sectionGroup.IsRecycleBin ? IconConstants.RecycleBin : IconConstants.SectionGroup;
		}
		public IconGeneratorInfo(OneNoteSection section)
		{
			Prefix = IconConstants.Section;
			Color = section.Color;
		}
		public IconGeneratorInfo(OneNotePage page)
		{
			Prefix = IconConstants.Page;
		}
	}
}