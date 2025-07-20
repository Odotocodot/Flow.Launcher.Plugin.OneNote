using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase
	{
		protected readonly PluginInitContext context;
		protected readonly Settings settings;
		protected readonly ResultCreator resultCreator;
		private readonly Func<string> keywordGetter;
		protected SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator, Func<string> keywordGetter)
		{
			this.context = context;
			this.settings = settings;
			this.resultCreator = resultCreator;
			this.keywordGetter = keywordGetter;
		}
		public string Keyword => keywordGetter();
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(string query);
	}
}