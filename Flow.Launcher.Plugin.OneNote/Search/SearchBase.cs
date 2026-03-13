using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator, Keyword? keyword)
	{
		protected readonly PluginInitContext context = context;
		protected readonly Settings settings = settings;
		protected readonly ResultCreator resultCreator = resultCreator;
#nullable disable
		public readonly Keyword keyword = keyword;
#nullable restore
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(Query query);

	}
}