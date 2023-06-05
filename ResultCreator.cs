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

        private readonly char[] notebookInvalidChars = "\\/*?\"|<>:%#.".ToCharArray();
        private readonly char[] sectionInvalidChars  = "\\/*?\"|<>:%#&".ToCharArray();

        public ResultCreator(PluginInitContext context, Settings settings)
        {
            this.settings = settings;
            this.context = context;
            notebookIcons = new OneNoteItemIcons(context, "Images/NotebookIcons", Icons.Notebook);
            sectionIcons = new OneNoteItemIcons(context, "Images/SectionIcons", Icons.Section);
        }

        private static string GetNicePath(IOneNoteItem item, bool removeSelf, string separator = " > ")
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

            path = path.Replace("/", separator).Replace("\\", separator);

            return path;
        }

        private string GetTitle(IOneNoteItem item, List<int> highlightData)
        {
            string title = item.Name;
            if (item.IsUnread && settings.ShowUnread)
            {
                string unread = "�  ";
                title = title.Insert(0, unread);

                if (highlightData != null)
                {
                    for (int i = 0; i < highlightData.Count; i++)
                    {
                        highlightData[i] += unread.Length;
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
                TitleToolTip = $"Created: {page.Created}\nLast Modified: {page.LastModified}",
                AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(page, false, "\\")}",
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

        private Result CreateSectionBaseResult(IOneNoteItem sectionBase, string iconPath, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            string path = GetNicePath(sectionBase, false);
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(sectionBase, false, "\\")}\\";

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
            return CreateSectionBaseResult(section, sectionIcons.GetIcon(section.Color.Value), actionIsAutoComplete, highlightData, score);
        }

        public Result CreateSectionGroupResult(OneNoteSectionGroup sectionGroup, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSectionBaseResult(sectionGroup, Icons.SectionGroup, actionIsAutoComplete, highlightData, score);
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
        public static Result CreateNewPageResult(string pageTitle, OneNoteSection section)
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
            bool validTitle = sectionTitle.IndexOfAny(sectionInvalidChars) == -1;

            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = validTitle
                        ? $"Path: {GetNicePath(parent, false)} > {sectionTitle}"
                        : $"Section names cannot contain: {string.Join(' ', sectionInvalidChars)}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    if(!validTitle)
                        return false;

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
            bool validTitle = sectionGroupTitle.IndexOfAny(sectionInvalidChars) == -1;

            return new Result
            {
                Title = $"Create section group: \"{sectionGroupTitle}\"",
                SubTitle = validTitle
                    ? $"Path: {GetNicePath(parent, false)} > {sectionGroupTitle}"
                    : $"Section group names cannot contain: {string.Join(' ', sectionInvalidChars)}",
                IcoPath = Icons.NewSectionGroup,
                Action = c =>
                {
                    if (!validTitle)
                        return false;

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
            bool validTitle = notebookTitle.IndexOfAny(notebookInvalidChars) == -1;

            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = validTitle 
                    ? $"Location: {oneNote.DefaultNotebookLocation}"
                    : $"Notebook names cannot contain: {string.Join(' ', notebookInvalidChars)}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    if (!validTitle) 
                        return false;

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