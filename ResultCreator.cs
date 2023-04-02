using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;

        private readonly OneNoteProvider oneNote;
        private readonly OneNoteItemInfo notebookInfo;
        private readonly OneNoteItemInfo sectionInfo;

        public ResultCreator(PluginInitContext context, OneNoteProvider oneNote)
        {
            this.context = context;
            this.oneNote = oneNote;
            notebookInfo = new OneNoteItemInfo("Images/NotebookIcons", Icons.Notebook, context);
            sectionInfo = new OneNoteItemInfo("Images/SectionIcons", Icons.Section, context);
        }
        
        private static string GetNicePath(IOneNoteItem item)
        {
            var path = item.RelativePath;

            if(path.EndsWith("/")  || path.EndsWith("\\"))
                path = path.Remove(path.Length - 1);

            if (path.EndsWith(".one"))
                path = path[..^4];
            path = path.Replace("/", " > ").Replace("\\", " > ");
            return path;

        }
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

        public Result CreatePageResult(OneNotePage page, List<int> highlightingData = null, int score = 0)
        {
            return new Result
            {
                Title = page.Name,
                TitleToolTip = $"Created: {page.DateTime}\nLast Modified: {page.LastModified}",
                TitleHighlightData = highlightingData,
                SubTitle = GetNicePath(page),
                Score = score,
                IcoPath = Icons.Logo,
                ContextData = page,
                Action = c =>
                {
                    oneNote.SyncItem(page);
                    oneNote.OpenInOneNote(page);
                    return true;
                },
            };
        }

        private Result CreateSecionBaseResult(IOneNoteItem sectionBase, string iconPath, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            string path = GetNicePath(sectionBase);
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{path.Replace(" > ","\\")}\\";

            return new Result
            {
                Title = sectionBase.Name,
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
                    oneNote.SyncItem(sectionBase);
                    oneNote.OpenInOneNote(sectionBase);
                    return true;
                }
            };
        }
        public Result CreateSectionResult(OneNoteSection section, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSecionBaseResult(section, sectionInfo.GetIcon(section.Color.Value), actionIsAutoComplete, highlightData, score);
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
                Title = notebook.Name,
                TitleToolTip = $"Number of sections: {notebook.Sections.Count()}",
                TitleHighlightData = highlightData,
                AutoCompleteText = autoCompleteText,
                ContextData = notebook,
                Score = score,
                IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    if (actionIsAutoComplete)
                    {
                        context.API.ChangeQuery(autoCompleteText);
                        return false;
                    }
                    oneNote.SyncItem(notebook);
                    oneNote.OpenInOneNote(notebook);
                    return true;
                }
            };
        }

        public Result CreateNewPageResult(string pageTitle, OneNoteSection section)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section)} > {pageTitle}",
                IcoPath = Icons.NewPage,
                Action = c =>
                {
                    oneNote.CreatePage(section, pageTitle);
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
                SubTitle = $"Path: {GetNicePath(parent)} > {sectionTitle}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    oneNote.CreateSection(parent, sectionTitle);
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
                SubTitle = $"Path: {GetNicePath(parent)} > {sectionGroupTitle}",
                IcoPath = Icons.NewSectionGroup,
                Action = c =>
                {
                    oneNote.CreateSectionGroup(parent, sectionGroupTitle);
                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

        public Result CreateNewNotebookResult(string notebookTitle)
        {
            notebookTitle = notebookTitle.Trim();
            return new Result
            {
                Title = $"Create notebook: \"{notebookTitle}\"",
                SubTitle = $"Location: {oneNote.DefaultNotebookLocation}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    oneNote.CreateNotebook(notebookTitle);
                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }
    }
}