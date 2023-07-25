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

        public ResultCreator(PluginInitContext context,  Settings settings)
        {
            this.settings = settings;
            this.context = context;
        }

        private static string GetNicePath(IOneNoteItem item, bool includeSelf = true, string separator = PathSeparator)
        {
            return item.RelativePath.Replace(OneNoteParser.RelativePathSeparator, separator);
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
                    $"Last Modified:\t{notebook.LastModified:F}\n"+
                    $"Sections:\t\t{notebook.Sections.Count()}\n"+
                    $"Sections Groups:\t{notebook.SectionGroups.Count()}";

                    subTitle = string.Empty;
                    autoCompleteText += Keyword.NotebookExplorerSeparator;
                    break;
                case OneNoteSectionGroup sectionGroup:
                    subTitleToolTip = $"{subTitle}\n\n" +
                    $"Last Modified:\t{sectionGroup.LastModified:F}\n" +
                    $"Sections:\t\t{sectionGroup.Sections.Count()}\n" +
                    $"Sections Groups:\t{sectionGroup.SectionGroups.Count()}";

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
                    $"Last Modified:\t{section.LastModified}\n"+
                    $"Pages:\t\t{section.Pages.Count()}";

                    autoCompleteText += Keyword.NotebookExplorerSeparator;
                    break;
                case OneNotePage page:
                    actionIsAutoComplete = false;
                    subTitle =  subTitle.Remove(subTitle.Length - (page.Name.Length + PathSeparator.Length));
                    subTitleToolTip = $"{subTitle}\n\n"+
                    $"Created:\t\t{page.Created:F}\n"+
                    $"Last Modified:\t{page.LastModified:F}";
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
                    OneNoteApplication.SyncItem(item);
                    OneNoteApplication.OpenInOneNote(item);
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
                    OneNoteApplication.CreatePage(section, pageTitle);
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

                    switch (parent)
                    {
                        case OneNoteNotebook notebook:
                            OneNoteApplication.CreateSection(notebook, sectionTitle);
                            break;
                        case OneNoteSectionGroup sectionGroup:
                            OneNoteApplication.CreateSection(sectionGroup, sectionTitle);
                            break;
                        default:
                            break;
                    }
                
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

                    switch (parent)
                    {
                        case OneNoteNotebook notebook:
                            OneNoteApplication.CreateSectionGroup(notebook, sectionGroupTitle);
                            break;
                        case OneNoteSectionGroup sectionGroup:
                            OneNoteApplication.CreateSectionGroup(sectionGroup, sectionGroupTitle);
                            break;
                        default:
                            break;
                    }

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        public Result CreateNewNotebookResult(string notebookTitle)
        {
            notebookTitle = notebookTitle.Trim();
            bool validTitle = OneNoteParser.IsNotebookTitleValid(notebookTitle);

            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = validTitle 
                    ? $"Location: {OneNoteApplication.GetDefaultNotebookLocation()}"
                    : $"Notebook names cannot contain: {string.Join(' ', OneNoteParser.InvalidNotebookChars)}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    if (!validTitle) 
                        return false;

                    OneNoteApplication.CreateNotebook(notebookTitle);

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