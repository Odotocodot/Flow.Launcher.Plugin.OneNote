using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;

        private const string PathSeparator = " > ";
        private const int RightAlignment = 5;

        public ResultCreator(PluginInitContext context,  Settings settings)
        {
            this.settings = settings;
            this.context = context;
        }

        private static string GetNicePath(IOneNoteItem item, bool includeSelf = true, string separator = PathSeparator)
        {
            return item.GetRelativePath(includeSelf, separator);
        }

        private string GetTitle(IOneNoteItem item, List<int> highlightData)
        {
            string title = item.Name;
            if (item.IsUnread && settings.ShowUnread)
            {
                string unread = "\u2022  ";
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
        public Result CreatePageResult(OneNotePage page, string query = null)
        {
            return CreateOneNoteItemResult(page, false, string.IsNullOrWhiteSpace(query) ? null : context.API.FuzzySearch(query, page.Name).MatchData);
        }

        public Result CreateOneNoteItemResult(IOneNoteItem item, bool actionIsAutoComplete, List<int> highlightData = null, int score = 0)
        {
            string title = GetTitle(item, highlightData);
            string titleToolTip = null;
            string subTitle = GetNicePath(item, true);
            string subTitleToolTip = null;
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {settings.NotebookExplorerKeyword}{GetNicePath(item, true, Keyword.NotebookExplorerSeparator)}";

            switch (item)
            {
                case OneNoteNotebook notebook:
                    titleToolTip = $"{notebook.Name}\n\n"+
                    $"Last Modified:\t{notebook.LastModified}\n\n"+
                    $"Sections:\t\t{notebook.Sections.Count(),RightAlignment}\n"+
                    $"Sections Groups:\t{notebook.SectionGroups.Count(),RightAlignment}";

                    subTitle = string.Empty;
                    autoCompleteText += Keyword.NotebookExplorerSeparator;
                    break;
                case OneNoteSectionGroup sectionGroup:
                    subTitleToolTip = $"{subTitle}\n\n" +
                    $"Last Modified:\t{sectionGroup.LastModified}\n\n" +
                    $"Sections:\t\t{sectionGroup.Sections.Count(),RightAlignment}\n" +
                    $"Sections Groups:\t{sectionGroup.SectionGroups.Count(),RightAlignment}";

                    autoCompleteText += Keyword.NotebookExplorerSeparator;                    
                    break;
                case OneNoteSection section:
                    if (section.Encrypted)
                    {
                        title += " [Encrypted]";
                        if (section.Locked)
                            title += "[Locked]";
                        else
                            title += "[Unlocked]";
                    }

                    subTitleToolTip = $"{subTitle}\n\n"+
                    $"Last Modified:\t{section.LastModified}\n\n"+
                    $"Pages:\t{section.Pages.Count(),RightAlignment}";

                    autoCompleteText += Keyword.NotebookExplorerSeparator;
                    break;
                case OneNotePage page:
                    actionIsAutoComplete = false;
                    subTitle =  subTitle.Remove(subTitle.Length - (page.Name.Length + PathSeparator.Length));
                    subTitleToolTip = $"{subTitle}\n\n"+
                    $"Created:\t\t{page.Created}\n"+
                    $"Last Modified:\t{page.LastModified}";
                    break;
            }
            return new Result
            {
                Title = title,
                TitleToolTip = titleToolTip,
                TitleHighlightData = highlightData,
                SubTitle = subTitle,
                SubTitleToolTip = subTitleToolTip,
                AutoCompleteText = autoCompleteText,
                Score = score,
                IcoPath = Icons.GetIcon(item),
                ContextData = item,
                Action = c =>
                {
                    if (actionIsAutoComplete)
                    {
                        context.API.ChangeQuery(autoCompleteText);
                        return false;
                    }
                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.SyncItem(item);
                        oneNote.OpenInOneNote(item);
                    });
                    return true;
                },
            };
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
                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreatePage(section, pageTitle);
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

                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        switch (parent)
                        {
                            case OneNoteNotebook notebook:
                                oneNote.CreateSection(notebook, sectionTitle);
                                break;
                            case OneNoteSectionGroup sectionGroup:
                                oneNote.CreateSection(sectionGroup, sectionTitle);
                                break;
                            default:
                                break;
                        }
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

                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        switch (parent)
                        {
                            case OneNoteNotebook notebook:
                                oneNote.CreateSectionGroup(notebook, sectionGroupTitle);
                                break;
                            case OneNoteSectionGroup sectionGroup:
                                oneNote.CreateSectionGroup(sectionGroup, sectionGroupTitle);
                                break;
                            default:
                                break;
                        }
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

                    OneNotePlugin.GetOneNote(oneNote =>
                    {
                        oneNote.CreateNotebook(notebookTitle);
                    });

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        #endregion
        
        public static List<Result> NoMatchesFoundResult()
        {
            return SingleResult("No matches found",
                                "Try searching something else, or syncing your notebooks.",
                                Icons.Logo);
        }
        public static List<Result> InvalidQuery()
        {
            return SingleResult("Invalid query",
                                "The first character of the search must be a letter or a digit",
                                Icons.Warning);
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