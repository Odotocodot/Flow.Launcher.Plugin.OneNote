using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flow.Launcher.Plugin.OneNote.Icons;
using Flow.Launcher.Plugin.OneNote.UI.Views;
using Humanizer;
using LinqToOneNote;
using LinqToOneNote.Abstractions;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly IconProvider iconProvider;

        private const string PathSeparator = " > ";
        private const string BulletPoint = "\u2022  ";
        private const string TrianglePoint = "\u2023  ";
        private string ActionKeyword => context.CurrentPluginMetadata.ActionKeyword;
        public ResultCreator(PluginInitContext context, Settings settings, IconProvider iconProvider)
        {
            this.settings = settings;
            this.iconProvider = iconProvider;
            this.context = context;
        }

        private static string GetNicePath(IOneNoteItem item, string separator = PathSeparator) => item.GetRelativePath(false, separator);

        private string GetTitle(IOneNoteItem item, List<int>? highlightData)
        {
            string title = item.Name;
            if (!item.IsUnread || !settings.ShowUnread) 
                return title;
            
            title = title.Insert(0, BulletPoint);

            if (highlightData == null)
                return title;
            
            for (int i = 0; i < highlightData.Count; i++)
            {
                highlightData[i] += BulletPoint.Length;
            }
            return title;
        }
        
        private string GetAutoCompleteText(IOneNoteItem item) //Auto complete text if in notebook explorer
        {
            string slash = item is Page ? string.Empty : Keywords.NotebookExplorerSeparator;
            return $"{ActionKeyword} {settings.Keywords.NotebookExplorer}{GetNicePath(item, Keywords.NotebookExplorerSeparator)}{slash}";
        }

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
                    AddSelectedCount = false,
                    Score = Result.MaxScore,
                },
                new Result
                {
                    Title = "View notebook explorer",
                    SubTitle = $"Type \"{settings.Keywords.NotebookExplorer}\" or select this option to search by notebook structure",
                    AutoCompleteText = $"{ActionKeyword} {settings.Keywords.NotebookExplorer}",
                    IcoPath = iconProvider.NotebookExplorer,
                    AddSelectedCount = false,
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
                    AddSelectedCount = false,
                    Score = -1000,
                    Action = _ =>
                    {
                        context.API.ChangeQuery($"{ActionKeyword} {settings.Keywords.RecentPages}", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = "New quick note",
                    IcoPath = iconProvider.QuickNote,
                    AddSelectedCount = false,
                    Score = -4000,
                    PreviewPanel = GetNewPagePreviewPanel(null, null),
                    Action = _ =>
                    {
                        OneNoteApp.CreateQuickNote(OpenMode.ExistingOrNewWindow);
                        WindowHelper.FocusOneNote();
                        return true;
                    },
                },
                new Result
                {
                    Title = "Open and sync notebooks",
                    IcoPath = iconProvider.Sync,
                    AddSelectedCount = false,
                    Score = int.MinValue,
                    Action = _ =>
                    {
                        var notebooks = OneNoteApp.GetFullHierarchy().Notebooks;
                        foreach (var notebook in notebooks)
                        {
                            notebook.Sync();
                        }

                        notebooks.GetAllPages()
                                 .Where(i => !i.IsInRecycleBin)
                                 .OrderByDescending(pg => pg.LastModified)
                                 .First()
                                 .Open();
                        
                        WindowHelper.FocusOneNote();
                        return true;
                    },
                },
            };
        }
        
        public Result CreateOneNoteItemResult(IOneNoteItem item, bool actionIsAutoComplete, List<int>? highlightData = null, int score = 0)
        {
            var title = GetTitle(item, highlightData);
            var toolTip = string.Empty;
            var subTitle = GetNicePath(item);
            var autoCompleteText = GetAutoCompleteText(item);
            var iconInfo = new IconGeneratorInfo(item);
            
            switch (item)
            {
                case INotebookOrSectionGroup i:
                    toolTip =
                        $"""
                         Last Modified:
                         {TrianglePoint}{i.LastModified:F}

                         Contains:
                         {TrianglePoint}{"section group".ToQuantity(i.SectionGroups.Count)}
                         {TrianglePoint}{"section".ToQuantity(i.Sections.Count)}
                         {TrianglePoint}{"page".ToQuantity(i.GetAllPages().Count())}
                         """;

                    if (i is Notebook)
                    {
                        subTitle = string.Empty;
                    }
                    break;
                case Section section:
                    if (section.Encrypted)
                    {
                        title += $" [Encrypted] {(section.Locked ? "[Locked]" : "[Unlocked]")}";
                    }

                    toolTip =
                        $"""
                         Last Modified:
                         {TrianglePoint}{section.LastModified:F}
                         
                         Contains:
                         {TrianglePoint}{"page".ToQuantity(section.GetAllPages().Count())}
                         """;
                    break;
                case Page page:
                    autoCompleteText = actionIsAutoComplete ? autoCompleteText[..^1] : string.Empty;

                    actionIsAutoComplete = false;

                    subTitle = subTitle[..^(page.Name.Length + PathSeparator.Length)];
                    toolTip =
                        $"""
                         {"Created:",-15}  {page.Created:F}
                         {"Last Modified:",-15}  {page.LastModified:F}
                         """;
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
                        item.Open();
                    });
                    WindowHelper.FocusOneNote();
                    return true;
                },
            };
        }
        
        public Result CreatePageResult(Page page, string query) 
            => CreateOneNoteItemResult(page, false, string.IsNullOrWhiteSpace(query) ? null : context.API.FuzzySearch(query, page.Name).MatchData);

        public Result CreateRecentPageResult(Page page)
        {
            var result = CreateOneNoteItemResult(page, false);
            result.SubTitle = $"{page.LastModified.Humanize()}  |  {result.SubTitle}";
            result.IcoPath = iconProvider.Recent;
            result.AddSelectedCount = false;
            return result;
        }

        //When name can have invalid chars
        private Result CreateNewItemResult<TNew, TParent>(string newName, TParent? parent, string iconPath, Func<TParent?, string, OpenMode, TNew> createFunc)
            where TNew : IOneNoteItem, INameInvalidCharacters
            where TParent : IOneNoteItem
            => CreateNewItemResult(newName, parent, iconPath, createFunc, OneNoteApp.IsValidName<TNew>(newName), TNew.InvalidCharacters);

        private Result CreateNewItemResult<TNew, TParent>(string newName, TParent? parent, string iconPath, Func<TParent?, string, OpenMode, TNew> createFunc, bool validTitle = true, IReadOnlyList<char>? invalidChars = null)
            where TNew : IOneNoteItem
            where TParent : IOneNoteItem
        {
            newName = newName.Trim();
            string type = nameof(TNew);
            return new Result
            {
                Title = $"Create {type.Transform(To.LowerCase)} \"{newName}\"",
                SubTitle = validTitle
                    ? parent == null // parent is null if trying to create a notebook
                        ? $"Location: {OneNoteApp.GetDefaultNotebookLocation()}" 
                        : $"Path: {GetNicePath(parent)}{PathSeparator}{newName}"
                    : $"{type.Transform(To.SentenceCase)} names cannot contain: {string.Join(' ', invalidChars!)}",
                AutoCompleteText = parent == null ? $"{ActionKeyword} {settings.Keywords.NotebookExplorer}{newName}" : $"{GetAutoCompleteText(parent)}{newName}",
                IcoPath = iconPath,
                Action = c =>
                {
                    if (!validTitle)
                        return false;

                    bool showOneNote = !c.SpecialKeyState.CtrlPressed;
                    createFunc(parent, newName, showOneNote ? OpenMode.ExistingOrNewWindow : OpenMode.None);

                    context.API.ReQuery();

                    if (showOneNote)
                        WindowHelper.FocusOneNote();

                    return showOneNote;
                },
            };
        }

        public Result CreateNewPageResult(string newPageName, Section section)
        {
            var result = CreateNewItemResult(newPageName, section, iconProvider.NewPage, OneNoteApp.CreatePage);
            result.PreviewPanel = GetNewPagePreviewPanel(section, newPageName);
            return result;
        }

        public Result CreateNewSectionResult(string newSectionName, INotebookOrSectionGroup parent) => CreateNewItemResult(newSectionName, parent, iconProvider.NewSection, OneNoteApp.CreateSection);

        public Result CreateNewSectionGroupResult(string newSectionGroupName, INotebookOrSectionGroup parent) => CreateNewItemResult(newSectionGroupName, parent, iconProvider.NewSectionGroup, OneNoteApp.CreateSectionGroup);

        public Result CreateNewNotebookResult(string newNotebookName) => CreateNewItemResult<Notebook, IOneNoteItem>(newNotebookName, null, iconProvider.NewNotebook, (_, name, openMode) => OneNoteApp.CreateNotebook(name, openMode));

        public List<Result> ContextMenu(Result selectedResult)
        {
            var results = new List<Result>();
            if (selectedResult.ContextData is IOneNoteItem item)
            {
                var result = CreateOneNoteItemResult(item, false);
                result.Title = $"Open and sync \"{item.Name}\"";
                result.SubTitle = string.Empty;
                result.Score = 30;
                result.AddSelectedCount = false;
                result.ContextData = null;
                results.Add(result);
                
                results.Add(new Result
                {
                    Title = "Open in new OneNote window",
                    IcoPath = IconProvider.Logo,
                    Score = 20,
                    AddSelectedCount = false,
                    Action = _ =>
                    {
                        OneNoteApp.Open(item, true);
                        WindowHelper.FocusOneNote();
                        return true;
                    }
                });

                string autoCompleteText = GetAutoCompleteText(item);
                results.Add(new Result
                {
                    Title = "Show in Notebook Explorer",
                    SubTitle = autoCompleteText,
                    AddSelectedCount = false,
                    Score = 10,
                    IcoPath = iconProvider.NotebookExplorer,
                    Action = _ =>
                    {
                        context.API.BackToQueryResults();
                        context.API.ChangeQuery(autoCompleteText, true);
                        return false;
                    }
                });
            }
            return results;
        }
        
        public List<Result> EmptyCollection(List<Result> results, IOneNoteItem? parent)
        {
            // parent can be null if the collection only contains notebooks.
            switch (parent)
            {
                case INotebookOrSectionGroup:
                    // Can create section/section group
                    results.Add(EmptyCollectionResult("section", iconProvider.NewSection, "(unencrypted) section"));
                    results.Add(EmptyCollectionResult("section group", iconProvider.NewSectionGroup));
                    break;
                case Section section:
                    // Can create page
                    if (!section.Locked)
                    {
                        results.Add(EmptyCollectionResult("page", iconProvider.NewPage, section: section));
                    }
                    break;
            }

            return results;

            Result EmptyCollectionResult(string title, string iconPath, string? subTitle = null, Section? section = null)
            {
                return new Result
                {
                    Title = $"Create {title}: \"\"",
                    SubTitle = $"No {subTitle ?? title}s found. Type a valid title to create one",
                    IcoPath = iconPath,
                    PreviewPanel = section != null ? GetNewPagePreviewPanel(section, null) : null ,
                };
            }
        }

        private Lazy<System.Windows.Controls.UserControl> GetNewPagePreviewPanel(Section? section, string? pageTitle)
            => new(() => new NewOneNotePagePreviewPanel(context, section, pageTitle));

        public static List<Result> NoMatchesFound()
        {
            return SingleResult("No matches found",
                                "Try searching something else, or syncing your notebooks",
                                IconProvider.Logo);
        }
        public List<Result> InvalidQuery(bool includeSubtitle = true)
        {
            return SingleResult("Invalid query",
                                includeSubtitle 
                                    ? "The first character of the search must be a letter or a digit"
                                    : string.Empty,
                                iconProvider.Warning);
        }
        public List<Result> SearchType(string title, string? parentName)
        {
            if (!string.IsNullOrWhiteSpace(parentName))
            {
                title += $" in \"{parentName}\"";
            }
            return SingleResult(title, string.Empty, iconProvider.Search);
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