
using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;

namespace Flow.Launcher.Plugin.OneNote
{
    public class ResultCreator
    {
        private PluginInitContext context;
        private OneNotePlugin oneNotePlugin;
        private OneNoteItemInfo notebookInfo;
        private OneNoteItemInfo sectionInfo;

        private IOneNoteExtNotebook LastSelectedNotebook { get => oneNotePlugin.lastSelectedNotebook; set => oneNotePlugin.lastSelectedNotebook = value; }
        private IOneNoteExtSection LastSelectedSection { get => oneNotePlugin.lastSelectedSection; set => oneNotePlugin.lastSelectedSection = value; }


        public ResultCreator(PluginInitContext context, OneNotePlugin oneNotePlugin)
        {
            this.context = context;
            this.oneNotePlugin = oneNotePlugin;
            notebookInfo = new OneNoteItemInfo("NotebookIcons", "notebook.png", context);
            sectionInfo = new OneNoteItemInfo("SectionIcons", "section.png", context);
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
            var path = GetNicePath(section, notebook, true);
            return new Result
            {
                Title = page.Name,
                SubTitle = path,
                TitleToolTip = $"Created: {page.DateTime}\nLast Modified: {page.LastModified}",
                SubTitleToolTip = path,
                IcoPath = Constants.LogoIconPath,
                ContextData = page,
                TitleHighlightData = highlightingData,
                Action = c =>
                {
                    LastSelectedNotebook = null;
                    LastSelectedSection = null;
                    page.OpenInOneNote();
                    return true;
                },
            };
        }

        public Result CreateSectionResult(IOneNoteExtSection section, IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            var path = GetNicePath(section, notebook, false);
            return new Result
            {
                Title = section.Name,
                SubTitle = path, // + " | " + section.Pages.Count().ToString(),
                TitleHighlightData = highlightData,
                ContextData = section,
                IcoPath = sectionInfo.GetIcon(section.Color.Value),
                Action = c =>
                {
                    LastSelectedSection = section;
                    context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}{LastSelectedNotebook.Name}\\{section.Name}\\");
                    return false;
                },
            };
        }

        public Result CreateNotebookResult(IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            return new Result
            {
                Title = notebook.Name,
                TitleHighlightData = highlightData,
                ContextData = notebook,
                IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    LastSelectedNotebook = notebook;
                    context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}{notebook.Name}\\");
                    return false;
                },
            };
        }
        public Result CreateNewPageResult(IOneNoteSection section, IOneNoteNotebook notebook, string pageTitle)
        {
            pageTitle = pageTitle.Trim();
            return new Result
            {
                Title = $"Create page: \"{pageTitle}\"",
                SubTitle = $"Path: {GetNicePath(section,notebook,true)}",
                //IcoPath = Constants.LogoIconPath,
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenPage(LastSelectedSection, pageTitle);
                    LastSelectedNotebook = null;
                    LastSelectedSection = null;
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
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenSection(LastSelectedNotebook,sectionTitle);
                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    LastSelectedNotebook = null;
                    LastSelectedSection = null;
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
                //IcoPath =
                Action = c =>
                {
                    ScipBeExtensions.CreateAndOpenNotebook(context,notebookTitle);
                    context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword);
                    LastSelectedNotebook = null;
                    LastSelectedSection = null;
                    return true;
                }
            };
        }

    }
}