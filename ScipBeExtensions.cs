using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Office.Interop.OneNote;
using ScipBe.Common.Office.OneNote;
using static Flow.Launcher.Plugin.OneNote.ScipBeUtils.Utils;

namespace Flow.Launcher.Plugin.OneNote
{
    public static class ScipBeExtensions
    {
        public static void OpenAndSync(this IOneNoteSection section)
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.NavigateTo(section.ID);
                oneNote.SyncHierarchy(section.ID);
            });
        }

        public static void OpenAndSync(this IOneNoteNotebook notebook)
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.NavigateTo(notebook.ID);
                oneNote.SyncHierarchy(notebook.ID);
            });
        }
        public static void OpenAndSync(this IEnumerable<IOneNoteNotebook> notebooks, IOneNotePage lastModifiedPage)
        {
            CallOneNoteSafely(oneNote =>
            {
                foreach (var notebook in notebooks)
                {
                    oneNote.SyncHierarchy(notebook.ID);
                }
                oneNote.NavigateTo(lastModifiedPage.ID);
            });
        }

        public static string GetDefaultNotebookLocation()
        {
            return CallOneNoteSafely(oneNote =>
            {
                oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
                return path;
            });
        }
        public static void CreateAndOpenPage(IOneNoteSection section, string pageTitle)
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.GetHierarchy(null, HierarchyScope.hsNotebooks, out string xmlNb);

                XNamespace ns = XDocument.Parse(xmlNb).Root.Name.Namespace;
                
                oneNote.CreateNewPage(section.ID, out string pageID, NewPageStyle.npsBlankPageWithTitle);

                oneNote.GetPageContent(pageID, out string xml, PageInfo.piBasic);
                XDocument doc = XDocument.Parse(xml);
                XElement Xtitle = doc.Descendants(ns + "T").First();
                Xtitle.Value = pageTitle;

                oneNote.UpdatePageContent(doc.ToString());

                oneNote.SyncHierarchy(pageID);
                oneNote.NavigateTo(pageID);
            });
        }

        public static void CreateAndOpenPage()
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
                oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
                
                oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);
                
                oneNote.SyncHierarchy(pageID);
                oneNote.NavigateTo(pageID);
            });
        }

        public static void CreateAndOpenSection(this IOneNoteNotebook notebook, string title)
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.OpenHierarchy(title + ".one", notebook.ID, out string sectionID, CreateFileType.cftSection);
                
                oneNote.SyncHierarchy(sectionID);
                oneNote.NavigateTo(sectionID);
            });
        }

        public static void CreateAndOpenNotebook(PluginInitContext context,string title)
        {
            CallOneNoteSafely(oneNote =>
            {
                oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
                
                oneNote.OpenHierarchy($"{path}\\{title}", null, out string notebookID, CreateFileType.cftNotebook);
                
                oneNote.SyncHierarchy(notebookID);
                oneNote.NavigateTo(notebookID);
            });
        }
    
    }
}
