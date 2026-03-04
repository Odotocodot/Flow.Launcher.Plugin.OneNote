using System.Drawing;
using LinqToOneNote;

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
				case Notebook n:
					prefix = IconConstants.Notebook;
					color = n.Color;
					break;
				case SectionGroup sg:
					prefix = sg.IsRecycleBin ? IconConstants.RecycleBin : IconConstants.SectionGroup;
					break;
				case Section s:
					prefix = IconConstants.Section;
					color = s.Color;
					break;
				case Page:
					prefix = IconConstants.Page;
					break;
			}
		}
	}
}