using System.Drawing;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
	public readonly struct IconGeneratorInfo
	{
		public readonly string prefix = string.Empty;
		public readonly Color? color;

		private IconGeneratorInfo(string prefix, Color? color = null)
		{
			this.prefix = prefix;
			this.color = color;
		}

		public static IconGeneratorInfo Create(IOneNoteItem item)
		{
			return item switch
			{
				Notebook n => new (IconConstants.Notebook, n.Color),
				SectionGroup sg => new (sg.IsRecycleBin ? IconConstants.RecycleBin : IconConstants.SectionGroup),
				Section s => new (IconConstants.Section, s.Color),
				Page => new (IconConstants.Page),
				_ => new (),
			};
		}
	}
}