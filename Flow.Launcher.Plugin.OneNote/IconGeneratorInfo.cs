using System.Drawing;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
	public record struct IconGeneratorInfo
	{
		public const string Notebook = "notebook";
		private const string SectionGroup = "section_group";
		private const string RecycleBin = "recycle_bin";
		public const string Section = "section";
		private const string Page = "page";
		public string Prefix { get; }
		public Color? Color { get; }
		
		public IconGeneratorInfo(OneNoteNotebook notebook)
		{
			Prefix = Notebook;
			Color = notebook.Color;
		}
		public IconGeneratorInfo(OneNoteSectionGroup sectionGroup)
		{
			Prefix = sectionGroup.IsRecycleBin ? RecycleBin : SectionGroup;
		}
		public IconGeneratorInfo(OneNoteSection section)
		{
			Prefix = Section;
			Color = section.Color;
		}
		public IconGeneratorInfo(OneNotePage page)
		{
			Prefix = Page;
		}
	}
}