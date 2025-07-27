using System.Drawing;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
	public struct IconGeneratorInfo
	{
		public readonly string prefix = string.Empty;
		public readonly Color? color;
		
		public IconGeneratorInfo(IOneNoteItem item)
		{
			switch (item)
			{
				case OneNoteNotebook n:
					prefix = IconConstants.Notebook;
					color = n.Color;
					break;
				case OneNoteSectionGroup sg:
					prefix = sg.IsRecycleBin ? IconConstants.RecycleBin : IconConstants.SectionGroup;
					break;
				case OneNoteSection s:
					prefix = IconConstants.Section;
					color = s.Color;
					break;
				case OneNotePage:
					prefix = IconConstants.Page;
					break;
			}
		}
	}
}