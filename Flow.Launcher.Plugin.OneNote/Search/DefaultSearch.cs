using System.Collections.Generic;
using System.Linq;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class DefaultSearch : SearchBase
	{
		public DefaultSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) 
			: base(context, settings, resultCreator, null) { }

		public override List<Result> GetResults(string query)
		{
			if (!char.IsLetterOrDigit(query[0]))
			{
				return resultCreator.InvalidQuery();
			}

			return OneNoteApp.FindPages(query)
			                 .Select(pg => resultCreator.CreatePageResult(pg, query))
			                 .ToList();

		}
	}
}