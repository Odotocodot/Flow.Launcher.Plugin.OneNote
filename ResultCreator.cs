using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private readonly PluginInitContext context;

        private readonly OneNoteItemIcons notebookIcons;
        private readonly OneNoteItemIcons sectionIcons;

        public ResultCreator(PluginInitContext context)
        {
            this.context = context;
            notebookIcons = new OneNoteItemIcons("Images/NotebookIcons", Icons.Notebook, context);
            sectionIcons = new OneNoteItemIcons("Images/SectionIcons", Icons.Section, context);
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
        public Result GetOneNoteItemResult(OneNoteProvider oneNote, IOneNoteItem item, bool actionIsAutoComplete, List<int> highlightData = null, int score = 0)
        {
            return item.ItemType switch
            {
                OneNoteItemType.Notebook => CreateNotebookResult(oneNote, (OneNoteNotebook)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.SectionGroup => CreateSectionGroupResult(oneNote, (OneNoteSectionGroup)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.Section => CreateSectionResult(oneNote, (OneNoteSection)item, actionIsAutoComplete, highlightData, score),
                OneNoteItemType.Page => CreatePageResult(oneNote, (OneNotePage)item, highlightData, score),
                _ => new Result(),
            };
        }

        public Result CreatePageResult(OneNoteProvider oneNote, OneNotePage page, List<int> highlightingData = null, int score = 0)
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
                    oneNote.Init();
                    oneNote.SyncItem(page);
                    oneNote.OpenInOneNote(page);
                    oneNote.Release();
                    return true;
                },
            };
        }

        private Result CreateSecionBaseResult(OneNoteProvider oneNote, IOneNoteItem sectionBase, string iconPath, bool actionIsAutoComplete, List<int> highlightData, int score)
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
                    oneNote.Init();
                    oneNote.SyncItem(sectionBase);
                    oneNote.OpenInOneNote(sectionBase);
                    oneNote.Release();

                    return true;
                }
            };
        }
        public Result CreateSectionResult(OneNoteProvider oneNote, OneNoteSection section, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSecionBaseResult(oneNote, section, sectionIcons.GetIcon(section.Color.Value), actionIsAutoComplete, highlightData, score);
        }

        public Result CreateSectionGroupResult(OneNoteProvider oneNote, OneNoteSectionGroup sectionGroup, bool actionIsAutoComplete, List<int> highlightData, int score)
        {
            return CreateSecionBaseResult(oneNote, sectionGroup, Icons.SectionGroup, actionIsAutoComplete, highlightData, score);
        }

        public Result CreateNotebookResult(OneNoteProvider oneNote, OneNoteNotebook notebook, bool actionIsAutoComplete, List<int> highlightData, int score)
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
                IcoPath = notebookIcons.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    if (actionIsAutoComplete)
                    {
                        context.API.ChangeQuery(autoCompleteText);
                        return false;
                    }
                    oneNote.Init();
                    oneNote.SyncItem(notebook); 
                    oneNote.OpenInOneNote(notebook);
                    oneNote.Release();
                    return true;
                }
            };
        }

        public Result CreateNewPageResult(OneNoteProvider oneNote, string pageTitle, OneNoteSection section)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section)} > {pageTitle}",
                IcoPath = Icons.NewPage,
                Action = c =>
                {
                    oneNote.Init();
                    oneNote.CreatePage(section, pageTitle);
                    oneNote.Release();
                    return true;
                }
            };
        }

        public Result CreateNewSectionResult(OneNoteProvider oneNote, string sectionTitle, IOneNoteItem parent)
        {
            sectionTitle = sectionTitle.Trim();
            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = $"Path: {GetNicePath(parent)} > {sectionTitle}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    oneNote.Init();
                    oneNote.CreateSection(parent, sectionTitle);
                    oneNote.Release();

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }
        public Result CreateNewSectionGroupResult(OneNoteProvider oneNote, string sectionGroupTitle, IOneNoteItem parent)
        {
            sectionGroupTitle = sectionGroupTitle.Trim();
            return new Result
            {
                Title = $"Create section group: \"{sectionGroupTitle}\"",
                SubTitle = $"Path: {GetNicePath(parent)} > {sectionGroupTitle}",
                IcoPath = Icons.NewSectionGroup,
                Action = c =>
                {
                    oneNote.Init();
                    oneNote.CreateSectionGroup(parent, sectionGroupTitle);
                    oneNote.Release();

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
                    oneNote.Init();
                    oneNote.CreateNotebook(notebookTitle);
                    oneNote.Release();

                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }
    }
}