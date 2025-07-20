using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class DefaultSearch : SearchBase
	{
		public override List<Result> GetResults(string query)
		{
			if (!char.IsLetterOrDigit(query[0]))
			{
				return resultCreator.InvalidQuery();
			}

			return OneNoteApplication.FindPages(query)
			                         .Select(pg => resultCreator.CreatePageResult(pg, query))
			                         .ToList();

		}
	}
}