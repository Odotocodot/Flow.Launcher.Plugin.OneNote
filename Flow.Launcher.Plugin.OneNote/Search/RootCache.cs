using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class RootCache
	{
		private bool isDirty;
		private Root? root;
		public Root Root
		{
			get
			{
				if (!isDirty && root is not null)
					return root;
				root = LinqToOneNote.OneNote.GetFullHierarchy();
				isDirty = false;
				return root;
			}
		}
		public void SetDirty()
		{
			isDirty = true;
		}
	}
}