using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly OneNoteItemIcons notebookIcons;
        private readonly OneNoteItemIcons sectionIcons;

        public ResultCreator(PluginInitContext context, Settings settings)
        {
            this.settings = settings;
            this.context = context;
            notebookIcons = new OneNoteItemIcons(context, "Images/NotebookIcons", Icons.Notebook);
            sectionIcons = new OneNoteItemIcons(context, "Images/SectionIcons", Icons.Section);
        }

        private static string GetNicePath(IOneNoteItem item, bool removeSelf)
        {
            var path = item.RelativePath;

            if (path.EndsWith(".one"))
                path = path[..^4];

            if (path.EndsWith("/") || path.EndsWith("\\"))
                path = path.Remove(path.Length - 1);

            if(removeSelf)
            {
                int index = path.LastIndexOf(item.Name);

                if (index != -1)
                {
                    path = path.Remove(index, item.Name.Length);
                    if (path.EndsWith("/") || path.EndsWith("\\"))
                        path = path.Remove(path.Length - 1);
                }
            }

            path = path.Replace("/", " > ").Replace("\\", " > ");

            return path;
        }

        private string GetTitle(IOneNoteItem item, List<int> hightlightData)
        {
            string title = item.Name;
            if (item.IsUnread && settings.ShowUnread)
            {
                string unread = "•  ";
                title = title.Insert(0, unread);

                if (hightlightData != null)
                {
                    for (int i = 0; i < hightlightData.Count; i++)
                    {
                        hightlightData[i] += unread.Length;
                    }
                }
            }
            return title;
        }

        #region Create OneNote Item Results
        public Result GetOneNoteItemResult(IOneNoteItem item, bool actionIsAutoComplete, List<int> highlightData = null, int score = 0)
        {
            return item.ItemType switch
            {
                OneNoteItemType.Notebook => CreateNotebookResult((OneNoteNotebook)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.SectionGroup => CreateSectionGroupResult((OneNoteSectionGroup)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.Section => CreateSectionResult((OneNoteSection)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.Page => CreatePageResult((OneNotePage)item, highlightData, score),
                _ => new Result(),
            };
        }
        public Result CreatePageResult(OneNotePage page, string query)
        {
            return CreatePageResult(page, context.API.FuzzySearch(query, page.Name).MatchData);
        }

        public Result CreatePageResult(OneNotePage page, List<int> highlightingData = null, int score = 0)
        {
            return new Result
            {
                Title = GetTitle(page, highlightingData),
                TitleToolTip = $"Created: {page.DateTime}\nLast Modified: {page.LastModified}",
                TitleHighlightData = highlightingData,
                SubTitle = GetNicePath(page, true),
                Score = score,
                IcoPath = Icons.Logo,
                ContextData = page,
                Action = c =>
                {
                    OpenOneNoteItem(page);
                    return true;
                },
            };
        }

        private Result CreateSecionBaseResult(IOneNoteItem sectionBase, string iconPath, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            string path = GetNicePath(sectionBase, false);
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{path.Replace(" > ","\\")}\\";

            return new Result
            {
                Title = GetTitle(sectionBase, highlightData),
                TitleHighlightData = highlightData,
                SubTitle = path,
                SubTitleToolTip = $"{path} | Number of pages: {sectionBase.Children.Count()}",
                AutoCompleteText = autoCompleteText,
                ContextData = sectionBase,
                Score = score,
                IcoPath = iconPath,
                Action = c =>
                {
                    if(actionIsAutoComplete)
                    {
                        context.API.ChangeQuery(autoCompleteText);
                        return false;
                    }
                    OpenOneNoteItem(sectionBase);

                    return true;
                }
            };
        }
        public Result CreateSectionResult(OneNoteSection section, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSecionBaseResult(section, sectionIcons.GetIcon(section.Color.Value), actionIsAutoComplete, highlightData, score);
        }

        public Result CreateSectionGroupResult(OneNoteSectionGroup sectionGroup, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSecionBaseResult(sectionGroup, Icons.SectionGroup, actionIsAutoComplete, highlightData, score);
        }

        public Result CreateNotebookResult(OneNoteNotebook notebook, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{notebook.Name}\\";

            return new Result
            {
                Title = GetTitle(notebook, highlightData),
                TitleToolTip = $"Number of sections: {notebook.Sections.Count()}",
                TitleHighlightData = highlightData,
                AutoCompleteText = autoCompleteText,
                ContextData = notebook,
                Score = score,
                IcoPath = notebookIcons.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    if (actionIsAutoComplete)
                    {
                        context.API.ChangeQuery(autoCompleteText);
                        return false;
                    }
                    OpenOneNoteItem(notebook);
                    return true;
                }
            };
        }

        private static void OpenOneNoteItem(IOneNoteItem item)
        {
            OneNotePlugin.GetOneNote(oneNote =>
            {
                oneNote.SyncItem(item);
                oneNote.OpenInOneNote(item);
                oneNote.Release();
                return 0;
            });
        }
        #endregion

        #region Create New OneNote Item Results
        public Result CreateNewPageResult(string pageTitle, OneNoteSection section)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section,false)} > {pageTitle}",
                IcoPath = Icons.NewPage,
                Action = c =>
                {
                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreatePage(section, pageTitle);
                        oneNote.Release();
                        return 0;
                    });
                    return true;
                }
            };
        }

        public Result CreateNewSectionResult(string sectionTitle, IOneNoteItem parent)
        {
            sectionTitle = sectionTitle.Trim();
            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = $"Path: {GetNicePath(parent, false)} > {sectionTitle}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreateSection(parent, sectionTitle);
                        oneNote.Release();
                        return 0;
                    });

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }
        public Result CreateNewSectionGroupResult(string sectionGroupTitle, IOneNoteItem parent)
        {
            sectionGroupTitle = sectionGroupTitle.Trim();
            return new Result
            {
                Title = $"Create section group: \"{sectionGroupTitle}\"",
                SubTitle = $"Path: {GetNicePath(parent, false)} > {sectionGroupTitle}",
                IcoPath = Icons.NewSectionGroup,
                Action = c =>
                {

                    OneNotePlugin.GetOneNote(oneNote => 
                    { 
                        oneNote.CreateSectionGroup(parent, sectionGroupTitle); 
                        oneNote.Release(); 
                        return 0; 
                    });

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        public Result CreateNewNotebookResult(OneNoteProvider oneNote, string notebookTitle)
        {
            notebookTitle = notebookTitle.Trim();
            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = $"Location: {oneNote.DefaultNotebookLocation}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreateNotebook(notebookTitle);
                        oneNote.Release();
                        return 0;
                    });

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        #endregion

        public List<Result> SearchByTitle(string query, IEnumerable<IOneNoteItem> currentCollection, IOneNoteItem parentItem = null)
        {
            if (query.Length == Keywords.SearchByTitle.Length)
            {
                var title = "Now searching by title";

                if (parentItem != null)
                    title += $" in \"{parentItem.Name}\"";

                return SingleResult(title, null, Icons.Search);
            }

            List<int> highlightData = null;
            int score = 0;
            var results = new List<Result>();

            var currentSearch = query[Keywords.SearchByTitle.Length..];

            results = currentCollection.Traverse(item =>
            {
                if (IsEncryptedSection(item))
                    return false;

                return FuzzySearch(item.Name, currentSearch, out highlightData, out score);
            })
            .Select(item => GetOneNoteItemResult(item, false, highlightData, score))
            .ToList();

            if (!results.Any())
                results = NoMatchesFoundResult();

            return results;
        }
        public bool FuzzySearch(string itemName, string searchString, out List<int> highlightData, out int score)
        {
            var matchResult = context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            score = matchResult.Score;
            return matchResult.IsSearchPrecisionScoreMet();
        }

        public static bool IsEncryptedSection(IOneNoteItem item)
        {
            if (item.ItemType == OneNoteItemType.Section)
            {
                return ((OneNoteSection)item).Encrypted;
            }
            return false;
        }

        public static List<Result> NoMatchesFoundResult()
        {
            return SingleResult("No matches found",
                                "Try searching something else, or syncing your notebooks.",
                                Icons.Logo);
        }

        public static List<Result> SingleResult(string title, string subTitle, string iconPath)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = title,
                    SubTitle = subTitle,
                    IcoPath = iconPath,
                }
            };
        }
    }
}