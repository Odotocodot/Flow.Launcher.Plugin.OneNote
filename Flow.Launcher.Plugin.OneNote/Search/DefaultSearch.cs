using System.Collections.Generic;
using System.Linq;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class DefaultSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		: SearchBase(context, settings, resultCreator, null)
	{
		public override List<Result> GetResults(Query query)
		{
			string search = query.Search;
			if (!char.IsLetterOrDigit(search[0]))
			{
				return resultCreator.InvalidQuery();
			}

			return OneNoteApp.FindPages(search)
			                 .Select(pg => resultCreator.CreatePageResult(pg, search))
			                 .ToList();
		}
	}
}