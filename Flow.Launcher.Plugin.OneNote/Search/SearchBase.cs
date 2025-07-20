using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase
	{
		protected readonly PluginInitContext context;
		protected readonly Settings settings;
		protected readonly ResultCreator resultCreator;
		public Func<string> KeywordGetter { get; init; }
		public string Keyword => KeywordGetter();
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(string query);
	}
}