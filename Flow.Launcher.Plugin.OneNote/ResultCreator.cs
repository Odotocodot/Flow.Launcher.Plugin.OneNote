using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.OneNote.Icons;
using Flow.Launcher.Plugin.OneNote.UI.Views;
using Humanizer;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly IconProvider iconProvider;

        private const string PathSeparator = " > ";
        private const string Unread = "\u2022  ";

        private string ActionKeyword => context.CurrentPluginMetadata.ActionKeyword;
        public ResultCreator(PluginInitContext context, Settings settings, IconProvider iconProvider)
        {
            this.settings = settings;
            this.iconProvider = iconProvider;
            this.context = context;
        }

        private static string GetNicePath(IOneNoteItem item, string separator = PathSeparator) =>
            item.RelativePath.Replace(OneNoteApplication.RelativePathSeparator.ToString(), separator);

        private string GetTitle(IOneNoteItem item, List<int> highlightData)
        {
            string title = item.Name;
            if (!item.IsUnread || !settings.ShowUnread) 
                return title;
            
            title = title.Insert(0, Unread);

            if (highlightData == null)
                return title;
            
            for (int i = 0; i < highlightData.Count; i++)
            {
                highlightData[i] += Unread.Length;
            }
            return title;
            
        }
        
        private string GetAutoCompleteText(IOneNoteItem item) 
            => $"{ActionKeyword} {settings.Keywords.NotebookExplorer}{GetNicePath(item, Keywords.NotebookExplorerSeparator)}{Keywords.NotebookExplorerSeparator}";


        public List<Result> EmptyQuery()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Search OneNote pages",
                    SubTitle = "Try typing something!",
                    AutoCompleteText = ActionKeyword,
                    IcoPath = iconProvider.Search,
                    Score = 5000,
                },
                new Result
                {
                    Title = "View notebook explorer",
                    SubTitle = $"Type \"{settings.Keywords.NotebookExplorer}\" or select this option to search by notebook structure ",
                    AutoCompleteText = $"{ActionKeyword} {settings.Keywords.NotebookExplorer}",
                    IcoPath = iconProvider.NotebookExplorer,
                    Score = 2000,
                    Action = _ =>
                    {
                        context.API.ChangeQuery($"{ActionKeyword} {settings.Keywords.NotebookExplorer}", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = "See recent pages",
                    SubTitle = $"Type \"{settings.Keywords.RecentPages}\" or select this option to see recently modified pages",
                    AutoCompleteText = $"{ActionKeyword} {settings.Keywords.RecentPages}",
                    IcoPath = iconProvider.Recent,
                    Score = -1000,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{ActionKeyword} {settings.Keywords.RecentPages}", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = "New quick note",
                    IcoPath = iconProvider.QuickNote,
                    Score = -4000,
                    PreviewPanel = new Lazy<UserControl>(() => new NewOneNotePagePreviewPanel(context, null, null)),
                    Action = c =>
                    {
                        OneNoteApplication.CreateQuickNote(true);
                        WindowHelper.FocusOneNote();
                        return true;
                    },
                },
                new Result
                {
                    Title = "Open and sync notebooks",
                    IcoPath = iconProvider.Sync,
                    Score = int.MinValue,
                    Action = c =>
                    {
                        var notebooks = OneNoteApplication.GetNotebooks();
                        foreach (var notebook in notebooks)
                        {
                            notebook.Sync();
                        }

                        notebooks.GetPages()
                                 .Where(i => !i.IsInRecycleBin)
                                 .OrderByDescending(pg => pg.LastModified)
                                 .First()
                                 .OpenInOneNote();
                        
                        WindowHelper.FocusOneNote();
                        return true;
                    },
                },
            };
        }
        
        public Result CreateOneNoteItemResult(IOneNoteItem item, bool actionIsAutoComplete, List<int> highlightData = null, int score = 0)
        {
            string title = GetTitle(item, highlightData);
            string toolTip = string.Empty;
            string subTitle = GetNicePath(item);
            string autoCompleteText = GetAutoCompleteText(item);

            IconGeneratorInfo iconInfo;
            
            switch (item)
            {
                case OneNoteNotebook notebook:
                    toolTip =
                        $"Last Modified:\t{notebook.LastModified:F}\n" +
                        $"Sections:\t\t{notebook.Sections.Count()}\n" +
                        $"Sections Groups:\t{notebook.SectionGroups.Count()}";

                    subTitle = string.Empty;
                    iconInfo = new IconGeneratorInfo(notebook);
                    break;
                case OneNoteSectionGroup sectionGroup:
                    toolTip =
                        $"Path:\t\t{subTitle}\n" +
                        $"Last Modified:\t{sectionGroup.LastModified:F}\n" +
                        $"Sections:\t\t{sectionGroup.Sections.Count()}\n" +
                        $"Sections Groups:\t{sectionGroup.SectionGroups.Count()}";

                    iconInfo = new IconGeneratorInfo(sectionGroup);
                    break;
                case OneNoteSection section:
                    if (section.Encrypted)
                    {
                        title += $" [Encrypted] {(section.Locked ? "[Locked]" : "[Unlocked]")}";
                    }

                    toolTip =
                        $"Path:\t\t{subTitle}\n" +
                        $"Last Modified:\t{section.LastModified}\n" +
                        $"Pages:\t\t{section.Pages.Count()}";
                    
                    iconInfo = new IconGeneratorInfo(section);
                    break;
                case OneNotePage page:
                    autoCompleteText = actionIsAutoComplete ? autoCompleteText[..^1] : string.Empty;

                    actionIsAutoComplete = false;

                    subTitle = subTitle[..^(page.Name.Length + PathSeparator.Length)];
                    toolTip =
                        $"Path:\t\t {subTitle} \n" +
                        $"Created:\t\t{page.Created:F}\n" +
                        $"Last Modified:\t{page.LastModified:F}";

                    iconInfo = new IconGeneratorInfo(page);
                    break;
                default:
                    iconInfo = default;
                    break;
            }

            return new Result
            {
                Title = title,
                TitleToolTip = toolTip,
                TitleHighlightData = highlightData,
                AutoCompleteText = autoCompleteText,
                SubTitle = subTitle,
                Score = score,
                Icon = iconProvider.GetIcon(iconInfo),
                ContextData = item,
                AsyncAction = async _ =>
                {
                    if (actionIsAutoComplete)
                    {
                        context.API.ChangeQuery($"{autoCompleteText}", true);
                        return false;
                    }

                    await Task.Run(() =>
                    {
                        item.Sync();
                        item.OpenInOneNote();
                    });
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }
        
        
        public Result CreatePageResult(OneNotePage page, string query) 
            => CreateOneNoteItemResult(page, false, string.IsNullOrWhiteSpace(query) ? null : context.API.FuzzySearch(query, page.Name).MatchData);

        public Result CreateRecentPageResult(OneNotePage page)
        {
            var result = CreateOneNoteItemResult(page, false, null);
            result.SubTitle = $"{page.LastModified.Humanize()}\t{result.SubTitle}";
            result.IcoPath = iconProvider.Recent;
            return result;
        }

        public Result CreateNewPageResult(string newPageName, OneNoteSection section)
        {
            newPageName = newPageName.Trim();
            return new Result
            {
                Title = $"Create page: \"{newPageName}\"",
                SubTitle = $"Path: {GetNicePath(section)}{PathSeparator}{newPageName}",
                AutoCompleteText = $"{GetAutoCompleteText}{newPageName}",
                IcoPath = iconProvider.NewPage,
                PreviewPanel = new Lazy<UserControl>(() => new NewOneNotePagePreviewPanel(context, section, newPageName)),
                Action = c =>
                {
                    OneNoteApplication.CreatePage(section, newPageName, true);
                    Main.ForceReQuery();
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }

        public Result CreateNewSectionResult(string newSectionName, IOneNoteItem parent)
        {
            newSectionName = newSectionName.Trim();
            bool validTitle = OneNoteApplication.IsSectionNameValid(newSectionName);

            return new Result
            {
                Title = $"Create section: \"{newSectionName}\"",
                SubTitle = validTitle
                        ? $"Path: {GetNicePath(parent)}{PathSeparator}{newSectionName}"
                        : $"Section names cannot contain: {string.Join(' ', OneNoteApplication.InvalidSectionChars)}",
                AutoCompleteText = $"{GetAutoCompleteText}{newSectionName}",
                IcoPath = iconProvider.NewSection,
                Action = c =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    switch (parent)
                    {
                        case OneNoteNotebook notebook:
                            OneNoteApplication.CreateSection(notebook, newSectionName, true);
                            break;
                        case OneNoteSectionGroup sectionGroup:
                            OneNoteApplication.CreateSection(sectionGroup, newSectionName, true);
                            break;
                    }

                    Main.ForceReQuery();
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }

        public Result CreateNewSectionGroupResult(string newSectionGroupName, IOneNoteItem parent)
        {
            newSectionGroupName = newSectionGroupName.Trim();
            bool validTitle = OneNoteApplication.IsSectionGroupNameValid(newSectionGroupName);

            return new Result
            {
                Title = $"Create section group: \"{newSectionGroupName}\"",
                SubTitle = validTitle
                    ? $"Path: {GetNicePath(parent)}{PathSeparator}{newSectionGroupName}"
                    : $"Section group names cannot contain: {string.Join(' ', OneNoteApplication.InvalidSectionGroupChars)}",
                AutoCompleteText = $"{GetAutoCompleteText}{newSectionGroupName}",
                IcoPath = iconProvider.NewSectionGroup,
                Action = c =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    switch (parent)
                    {
                        case OneNoteNotebook notebook:
                            OneNoteApplication.CreateSectionGroup(notebook, newSectionGroupName, true);
                            break;
                        case OneNoteSectionGroup sectionGroup:
                            OneNoteApplication.CreateSectionGroup(sectionGroup, newSectionGroupName, true);
                            break;
                    }

                    Main.ForceReQuery();
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }

        public Result CreateNewNotebookResult(string newNotebookName)
        {
            newNotebookName = newNotebookName.Trim();
            bool validTitle = OneNoteApplication.IsNotebookNameValid(newNotebookName);

            return new Result
            {
                Title = $"Create notebook: \"{newNotebookName}\"",
                SubTitle = validTitle
                    ? $"Location: {OneNoteApplication.GetDefaultNotebookLocation()}"
                    : $"Notebook names cannot contain: {string.Join(' ', OneNoteApplication.InvalidNotebookChars)}",
                AutoCompleteText = $"{GetAutoCompleteText}{newNotebookName}",
                IcoPath = iconProvider.NewNotebook,
                Action = c =>
                {
                    if (!validTitle)
                    {
                        return false;
                    }

                    OneNoteApplication.CreateNotebook(newNotebookName, true);
                    Main.ForceReQuery();
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }
        
        public List<Result> ContextMenu(Result selectedResult)
        {
            var results = new List<Result>();
            if (selectedResult.ContextData is IOneNoteItem item)
            {
                var result = CreateOneNoteItemResult(item, false);
                result.Title = $"Open and sync \"{item.Name}\"";
                result.SubTitle = string.Empty;
                result.ContextData = null;
                results.Add(result);

                if (item is not OneNotePage)
                {
                    results.Add(new Result
                    {
                        Title = "Show in Notebook Explorer",
                        SubTitle = result.AutoCompleteText,
                        Score = - 1000,
                        IcoPath = iconProvider.NotebookExplorer,
                        Action = _ =>
                        {
                            context.API.ChangeQuery(result.AutoCompleteText);
                            return false;
                        }
                    });
                }
            }
            return results;
        }
        public List<Result> NoItemsInCollection(List<Result> results, IOneNoteItem parent)
        {
            // parent can be null if the collection only contains notebooks.
            switch (parent)
            {
                case OneNoteNotebook:
                case OneNoteSectionGroup:
                    // Can create section/section group
                    results.Add(NoItemsInCollectionResult("section", iconProvider.NewSection, "(unencrypted) section"));
                    results.Add(NoItemsInCollectionResult("section group", iconProvider.NewSectionGroup));
                    break;
                case OneNoteSection section:
                    // Can create page
                    if (!section.Locked)
                    {
                        results.Add(NoItemsInCollectionResult("page", iconProvider.NewPage, section: section));
                    }
                    break;
            }

            return results;

            Result NoItemsInCollectionResult(string title, string iconPath, string subTitle = null, OneNoteSection section = null)
            {
                return new Result
                {
                    Title = $"Create {title}: \"\"",
                    SubTitle = $"No {subTitle ?? title}s found. Type a valid title to create one",
                    IcoPath = iconPath,
                    PreviewPanel = section != null ? new Lazy<UserControl>(() => new NewOneNotePagePreviewPanel(context, section, null)) : null ,
                };
            }
        }
        public static List<Result> NoMatchesFound()
        {
            return SingleResult("No matches found",
                                "Try searching something else, or syncing your notebooks.",
                                IconProvider.Logo);
        }
        public List<Result> InvalidQuery()
        {
            return SingleResult("Invalid query",
                                "The first character of the search must be a letter or a digit",
                                iconProvider.Warning);
        }
        public List<Result> SearchingByTitle()
        {
            return SingleResult($"Now searching by title.", null, iconProvider.Search);
        }

        private static List<Result> SingleResult(string title, string subTitle, string iconPath)
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