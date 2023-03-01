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

        public static void Sync(this IOneNotePage item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNotePage> items)
        {
            CallOneNoteSafely(onenote =>
            {
                foreach (var item in items)
                {
                    onenote.SyncHierarchy(item.ID);
                }
            });
        }

        public static void Sync(this IOneNoteSection item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNoteSection> items)
        {
            CallOneNoteSafely(onenote =>
            {
                foreach (var item in items)
                {
                    onenote.SyncHierarchy(item.ID);
                }
            });
        }

        public static void Sync(this IOneNoteNotebook item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNoteNotebook> items)
        {
            CallOneNoteSafely(onenote =>
            {
                foreach (var item in items)
                {
                    onenote.SyncHierarchy(item.ID);
                }
            });
        }

        public static string GetDefaultNotebookLocation()
        {
            return CallOneNoteSafely(onenote =>
            {
                onenote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
                return path;
            });
        }

        public static string GetDefaultPageLocation()
        {
            return CallOneNoteSafely(onenote =>
            {
                onenote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
                return path;
            });
        }

        public static void CreateAndOpenPage(IOneNoteSection section, string pageTitle)
        {
            CallOneNoteSafely(onenote =>
            {
                onenote.GetHierarchy(null, HierarchyScope.hsNotebooks, out string xmlNb);

                XNamespace ns = XDocument.Parse(xmlNb).Root.Name.Namespace;
                
                onenote.CreateNewPage(section.ID, out string pageID, NewPageStyle.npsBlankPageWithTitle);

                onenote.GetPageContent(pageID, out string xml, PageInfo.piBasic);
                var doc = XDocument.Parse(xml);
                var Xtitle = doc.Descendants(ns + "T").First();
                Xtitle.Value = pageTitle;

                onenote.UpdatePageContent(doc.ToString());

                onenote.SyncHierarchy(pageID);
                onenote.NavigateTo(pageID);
            });
        }

        public static void CreateAndOpenSection(this IOneNoteNotebook notebook, string title)
        {
            CallOneNoteSafely(onenote =>
            {
                onenote.OpenHierarchy(title + ".one", notebook.ID, out string sectionID, CreateFileType.cftSection);
                
                onenote.SyncHierarchy(sectionID);
                onenote.NavigateTo(sectionID);
            });
        }

        public static void CreateAndOpenNotebook(PluginInitContext context,string title)
        {
            CallOneNoteSafely(onenote =>
            {
                onenote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
                
                onenote.OpenHierarchy($"{path}\\{title}", null, out string notebookID, CreateFileType.cftNotebook);
                
                onenote.SyncHierarchy(notebookID);
                onenote.NavigateTo(notebookID);
            });
        }
    
    }
}
