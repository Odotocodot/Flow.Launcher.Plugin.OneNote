using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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

        private static string GetNicePath(IOneNoteItem item, bool includeSelf = true, string separator = " > ")
        {
            return item.GetRelativePath(includeSelf, separator);
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

        public Result CreateItemBaseResult(IOneNoteItem item, bool actionIsAutoComplete, List<int> highlightData = null, int score = 0)
        {
            string titleToolTip = null;
            string subTitleToolTip = null;
            string autoCompleteText = null;
            string subTitle = null;
            string iconPath = null;
            Func<ActionContext, bool> action = null;
            switch (item.ItemType)
            {
                case OneNoteItemType.Notebook:
                    OneNoteNotebook notebook = (OneNoteNotebook)item;
                    titleToolTip = $"{"Number of sections:",-26} {notebook.Sections.Count(),4}\n{"Number of sections groups:",-26} {notebook.SectionGroups.Count(),4}";
                    iconPath = notebookIcons.GetIcon(notebook.Color.Value),
                    action = c =>
                    {
                        if (actionIsAutoComplete)
                        {
                            context.API.ChangeQuery(autoCompleteText);
                            return false;
                        }
                        OpenOneNoteItem(notebook);

                        return true;
                    };
                    break;
                case OneNoteItemType.SectionGroup:
                    OneNoteSectionGroup sectionGroup = (OneNoteSectionGroup)item;
                    subTitle = GetNicePath(sectionGroup, true);
                    autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(sectionGroup, true, Keywords.NotebookExplorerSeparator)}{Keywords.NotebookExplorerSeparator}";
                    subTitleToolTip = $"{subTitle}\n{"Number of sections:",-26} {sectionGroup.Sections.Count(),4}\n{"Number of sections groups:",-26} {sectionGroup.SectionGroups.Count(),4}";
                    iconPath = Icons.SectionGroup;
                    action = c =>
                    {
                        if (actionIsAutoComplete)
                        {
                            context.API.ChangeQuery(autoCompleteText);
                            return false;
                        }
                        OpenOneNoteItem(sectionGroup);
                        return true;
                    };
                    break;
                case OneNoteItemType.Section:
                    OneNoteSection section = (OneNoteSection)item;
                    subTitle = GetNicePath(section, true);
                    autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(section, true, Keywords.NotebookExplorerSeparator)}{Keywords.NotebookExplorerSeparator}";
                    subTitleToolTip = $"{subTitle}\nNumber of pages: {section.Pages.Count()}";
                    iconPath = sectionIcons.GetIcon(section.Color.Value);
                    action = c =>
                    {
                        if (actionIsAutoComplete)
                        {
                            context.API.ChangeQuery(autoCompleteText);
                            return false;
                        }
                        OpenOneNoteItem(section);

                        return true;
                    };
                    break;
                case OneNoteItemType.Page:
                    OneNotePage page = (OneNotePage)item;
                    subTitleToolTip = $"{"Created:",-18} {page.Created,20}\n{"Last Modified:",-18} {page.LastModified,20}";
                    autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(page, true, Keywords.NotebookExplorerSeparator)}";
                    subTitle = GetNicePath(page, false);
                    iconPath = Icons.Logo;
                    action = c =>
                    {
                        OpenOneNoteItem(page);
                        return true;
                    };
                    break;
                default:
                    break;
            }
            return new Result
            {
                Title = GetTitle(item, highlightData),
                TitleToolTip = titleToolTip,
                TitleHighlightData = highlightData,
                SubTitle = subTitle,
                SubTitleToolTip = subTitleToolTip,
                AutoCompleteText = autoCompleteText,
                Score = score,
                IcoPath = iconPath,
                ContextData = item,
                Action = action,
            };
        }
        public Result CreatePageResult(OneNotePage page, List<int> highlightingData = null, int score = 0)
        {
            return new Result
            {
                Title = GetTitle(page, highlightingData),
                TitleToolTip = $"{"Created:",-18} {page.Created,20}\n{"Last Modified:",-18} {page.LastModified,20}",
                AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(page, true, Keywords.NotebookExplorerSeparator)}",
                TitleHighlightData = highlightingData,
                SubTitle = GetNicePath(page,false),
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
            string path = GetNicePath(sectionBase, true);
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{GetNicePath(sectionBase, true, Keywords.NotebookExplorerSeparator)}{Keywords.NotebookExplorerSeparator}";

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
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{notebook.Name}{Keywords.NotebookExplorerSeparator}";

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
                SubTitle = $"Path: {GetNicePath(section, true)} > {pageTitle}",
                IcoPath = Icons.NewPage,
                Action = c =>
                {
                    _ = OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreatePage(section, pageTitle);
                        return 0;
                    });
                    return true;
                }
            };
        }

        public Result CreateNewSectionResult(string sectionTitle, IOneNoteItem parent)
        {
            sectionTitle = sectionTitle.Trim();
            bool validTitle = OneNoteParser.IsSectionTitleValid(sectionTitle);

            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = validTitle
                        ? $"Path: {GetNicePath(parent, true)} > {sectionTitle}"
                        : $"Section names cannot contain: {string.Join(' ', OneNoteParser.InvalidSectionChars)}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    if(!validTitle)
                        return false;

                    _ = OneNotePlugin.GetOneNote(oneNote =>
                    {
                        switch (parent.ItemType)
                        {
                            case OneNoteItemType.Notebook:
                                oneNote.CreateSection((OneNoteNotebook)parent, sectionTitle);
                                break;
                            case OneNoteItemType.SectionGroup:
                                oneNote.CreateSection((OneNoteSectionGroup)parent, sectionTitle);
                                break;
                            default:
                                break;
                        }
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
            bool validTitle = OneNoteParser.IsSectionGroupTitleValid(sectionGroupTitle);

            return new Result
            {
                Title = $"Create section group: \"{sectionGroupTitle}\"",
                SubTitle = validTitle
                    ? $"Path: {GetNicePath(parent, true)} > {sectionGroupTitle}"
                    : $"Section group names cannot contain: {string.Join(' ', OneNoteParser.InvalidSectionGroupChars)}",
                IcoPath = Icons.NewSectionGroup,
                Action = c =>
                {
                    if (!validTitle)
                        return false;

                    _ = OneNotePlugin.GetOneNote(oneNote =>
                    {
                        switch (parent.ItemType)
                        {
                            case OneNoteItemType.Notebook:
                                oneNote.CreateSectionGroup((OneNoteNotebook)parent, sectionGroupTitle);
                                break;
                            case OneNoteItemType.SectionGroup:
                                oneNote.CreateSectionGroup((OneNoteSectionGroup)parent, sectionGroupTitle);
                                break;
                            default:
                                break;
                        }
                        return 0;
                    });

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        public Result CreateNewNotebookResult(OneNoteApplication oneNote, string notebookTitle)
        {
            notebookTitle = notebookTitle.Trim();
            bool validTitle = OneNoteParser.IsNotebookTitleValid(notebookTitle);

            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = validTitle 
                    ? $"Location: {oneNote.GetDefaultNotebookLocation()}"
                    : $"Notebook names cannot contain: {string.Join(' ', OneNoteParser.InvalidNotebookChars)}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    if (!validTitle) 
                        return false;

                    _ = OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreateNotebook(notebookTitle);
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


        //public static IEnumerable<IOneNoteItem> RemoveEncrypted(this IEnumerable<IOneNoteItem> items)
        //{
        //    return items.Where(item =>
        //    {
        //        if (item.ItemType == OneNoteItemType.Section)
        //        {
        //            return !((OneNoteSection)item).Encrypted;
        //        }
        //        return true;
        //    });
        //}
        //public static IEnumerable<IOneNoteItem> SettingsCheck(this IEnumerable<IOneNoteItem> items)
        //{
        //    return items.Where(item =>
        //    {
        //          if setting.showEncrypted, 
        //          if setting.ShowRecyclebin
        //        if (item.ItemType == OneNoteItemType.Section)
        //        {
        //            return !((OneNoteSection)item).Encrypted;
        //        }
        //        return true;
        //    });
        //}

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