
using System;
using System.Collections.Generic;
using System.Linq;
using ScipBe.Common.Office.OneNote;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private PluginInitContext context;
        private OneNoteItemInfo notebookInfo;
        private OneNoteItemInfo sectionInfo;

        public ResultCreator(PluginInitContext context)
        {
            this.context = context;
            notebookInfo = new OneNoteItemInfo("Images/NotebookIcons", Icons.Notebook, context);
            sectionInfo = new OneNoteItemInfo("Images/SectionIcons", Icons.Section, context);
        }
        
        private string GetNicePath(IOneNoteSection section, IOneNoteNotebook notebook, bool isPage)
        {
            int offset = isPage 
                ? 4 //"4" is to remove the ".one" from the path
                : section.Name.Length + 5; //The "+5" is to remove the ".one" and "/" from the path
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index..^offset]
                    .Replace("/", " > ")
                    .Replace("\\", " > ");
            return path;
        }
        
        
        public Result CreatePageResult(IOneNoteExtPage page, List<int> highlightingData = null)
        {
            return CreatePageResult(page, page.Section, page.Notebook, highlightingData);
        }

        public Result CreatePageResult(IOneNotePage page, IOneNoteSection section, IOneNoteNotebook notebook, List<int> highlightingData = null)
        {
            return new Result
            {
                Title = page.Name,
                TitleToolTip = $"Created: {page.DateTime}\nLast Modified: {page.LastModified}",
                TitleHighlightData = highlightingData,
                SubTitle = GetNicePath(section, notebook, true),
                IcoPath = Icons.Logo,
                ContextData = page,
                Action = c =>
                {
                    page.OpenInOneNote();
                    return true;
                },
            };
        }

        public Result CreateSectionResult(IOneNoteExtSection section, IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            string path = GetNicePath(section, notebook, false);
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{notebook.Name}\\{section.Name}\\";
            return new Result
            {
                Title = section.Name,
                TitleHighlightData = highlightData,
                SubTitle = path,
                SubTitleToolTip = $"{path} | Number of pages: {section.Pages.Count()}",
                AutoCompleteText = autoCompleteText,
                ContextData = (section, notebook),
                IcoPath = sectionInfo.GetIcon(section.Color.Value),
            };
        }

        public Result CreateNotebookResult(IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            string autoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Keywords.NotebookExplorer}{notebook.Name}\\";
            return new Result
            {
                Title = notebook.Name,
                TitleToolTip = $"Number of sections: {notebook.Sections.Count()}",
                TitleHighlightData = highlightData,
                AutoCompleteText = autoCompleteText,
                ContextData = notebook,
                IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
            };
        }
        
        
        public Result CreateNewPageResult(IOneNoteSection section, IOneNoteNotebook notebook, string pageTitle)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section,notebook,true)}",
                IcoPath = Icons.NewPage,
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenPage(section, pageTitle);
                    return true;
                }
            };
        }

        public Result CreateNewSectionResult(IOneNoteNotebook notebook, string sectionTitle)
        {
            sectionTitle = sectionTitle.Trim();
            return new Result
            {
                Title = $"Create section: \"{sectionTitle}\"",
                SubTitle = $"Path: {notebook.Name}",
                IcoPath = Icons.NewSection,
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenSection(notebook,sectionTitle);
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
                //TitleHighlightData = context.API.FuzzySearch(notebookTitle,title).MatchData,
                SubTitle = $"Location: {ScipBeExtensions.GetDefaultNotebookLocation()}",
                IcoPath = Icons.NewNotebook,
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenNotebook(context,notebookTitle);
                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    return true;
                }
            };
        }

    }
}