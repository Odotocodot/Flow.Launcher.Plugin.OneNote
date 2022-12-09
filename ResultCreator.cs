
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;

namespace Flow.Launcher.Plugin.OneNote
{
    public static class ResultCreator
    {
        private static OneNoteItemInfo notebookInfo;
        private static OneNoteItemInfo sectionInfo;

        static ResultCreator()
        {
            notebookInfo = new OneNoteItemInfo("NotebookIcons", "notebook.png", OneNote.Context);
            sectionInfo = new OneNoteItemInfo("SectionIcons", "section.png", OneNote.Context);
        }
        public static Result CreateResult(this IOneNoteExtPage page, List<int> highlightingData = null)
        {
            return CreateResult(page, page.Section, page.Notebook, highlightingData);
        }

        public static Result CreateResult(this IOneNotePage page, IOneNoteSection section, IOneNoteNotebook notebook, List<int> highlightingData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index..^4] //"+4" is to remove the ".one" from the path
                    .Replace("/", " > ")
                    .Replace("\\", " > "); 
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
                    OneNote.LastSelectedNotebook = null;
                    OneNote.LastSelectedSection = null;
                    page.OpenInOneNote();
                    return true;
                },
            };
        }

        public static Result CreateResult(this IOneNoteExtSection section, IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            var sectionPath = section.Path;
            var index = sectionPath.IndexOf(notebook.Name);
            var path = sectionPath[index..^(section.Name.Length + 5)] //The "+5" is to remove the ".one" and "/" from the path
                    .Replace("/", " > ")
                    .Replace("\\", " > "); 
            
            return new Result
            {
                Title = section.Name,
                SubTitle = path, // + " | " + section.Pages.Count().ToString(),
                TitleHighlightData = highlightData,
                ContextData = section,
                IcoPath = sectionInfo.GetIcon(section.Color.Value),
                Action = c =>
                {
                    OneNote.LastSelectedSection = section;
                    OneNote.Context.API.ChangeQuery($"{OneNote.Context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}{OneNote.LastSelectedNotebook.Name}\\{section.Name}\\");
                    return false;
                },
            };
        }

        public static Result CreateResult(this IOneNoteExtNotebook notebook, List<int> highlightData = null)
        {
            return new Result
            {
                Title = notebook.Name,
                TitleHighlightData = highlightData,
                ContextData = notebook,
                IcoPath = notebookInfo.GetIcon(notebook.Color.Value),
                Action = c =>
                {
                    OneNote.LastSelectedNotebook = notebook;
                    OneNote.Context.API.ChangeQuery($"{OneNote.Context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}{notebook.Name}\\");
                    return false;
                },
            };
        }
    }
}