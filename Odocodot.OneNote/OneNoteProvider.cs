using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote
{
    /// <summary>
    /// OneNote Provider (LINQ to OneNote).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Author: Stefan Cruysberghs</item>
    /// <item>Website: http://www.scip.be</item>
    /// <item>Article: Querying Outlook and OneNote with LINQ : http://www.scip.be/index.php?Page=ArticlesNET05</item>
    /// </list>
    /// </remarks>
    public static class OneNoteProvider
    {
        /// <summary>
        /// Hierarchy of Notebooks with Sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebookExt> GetNotebooks(Application oneNote)
        {
            // Get OneNote hierarchy as XML document
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string oneNoteXMLHierarchy);
            var oneNoteHierarchy = XElement.Parse(oneNoteXMLHierarchy);
            var one = oneNoteHierarchy.GetNamespaceOfPrefix("one");

            // Transform XML into object hierarchy
            return oneNoteHierarchy.Elements(one + "Notebook")
                                   .Where(n => n.HasAttributes)
                                   .Select(n => ParseNotebook(n, one));
        }

        /// <summary>
        /// Collection of Pages.
        /// </summary>
        public static IEnumerable<OneNotePage> GetPages(Application oneNote)
        {
            // Get OneNote hierarchy as XML document
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string oneNoteXMLHierarchy);
            return ParsePages(oneNoteXMLHierarchy);
        }

        /// <summary>
        /// Returns a list of pages that match the specified query term. <br/>
        /// Pass exactly the same string that you would type into the search box in the OneNote UI. You can use bitwise operators, such as AND and OR, which must be all uppercase.
        /// </summary>
        /// <param name="searchString"></param>
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, string searchString)
        {
            oneNote.FindPages(null, searchString, out string xml);
            return ParsePages(xml);
        }

        private static OneNoteNotebook ParseNotebook(XElement notebookElement)
        {
            var notebook = new OneNoteNotebook
            {
                ID = notebookElement.Attribute("ID").Value,
                Name = notebookElement.Attribute("name").Value,
                NickName = notebookElement.Attribute("nickname").Value,
                Path = notebookElement.Attribute("path").Value,
                IsUnread = notebookElement.Attribute("isUnread") != null && (bool)notebookElement.Attribute("isUnread"),
                Color = notebookElement.Attribute("color").Value != "none" ? ColorTranslator.FromHtml(notebookElement.Attribute("color").Value) : null,
            };
            return notebook;
        }
        private static OneNoteNotebookExt ParseNotebook(XElement notebookElement, XNamespace oneNamespace)
        {
            var notebook = new OneNoteNotebookExt
            {
                ID = notebookElement.Attribute("ID").Value,
                Name = notebookElement.Attribute("name").Value,
                NickName = notebookElement.Attribute("nickname").Value,
                Path = notebookElement.Attribute("path").Value,
                IsUnread = notebookElement.Attribute("isUnread") != null && (bool)notebookElement.Attribute("isUnread"),
                Color = notebookElement.Attribute("color").Value != "none" ? ColorTranslator.FromHtml(notebookElement.Attribute("color").Value) : null,
            };
            notebook.Sections = notebookElement.Elements()
                                               .Where(s => s.Name.LocalName == "Section" || s.Name.LocalName == "SectionGroup")
                                               .Select(s => ParseSectionBase(s, oneNamespace, notebook, notebook));
            return notebook;
        }

        private static IOneNoteSectionBase ParseSectionBase(XElement xElement, XNamespace oneNamespace, OneNoteNotebookExt notebook, IOneNoteItem parent)
        {
            if (xElement.Name.LocalName == "Section")
                return ParseSection(xElement, oneNamespace, notebook, parent);
            else
                return ParseSectionGroup(xElement, oneNamespace, notebook, parent);
        }

        private static OneNoteSectionGroup ParseSectionGroup(XElement sectionGroupElement, XNamespace oneNamespace, OneNoteNotebookExt notebook, IOneNoteItem parent)
        {
            var sectionGroup = new OneNoteSectionGroup
            {
                ID = sectionGroupElement.Attribute("ID").Value,
                Name = sectionGroupElement.Attribute("name").Value,
                Path = sectionGroupElement.Attribute("path").Value,
                IsUnread = sectionGroupElement.Attribute("isUnread") != null && (bool)sectionGroupElement.Attribute("isUnread"),
                Notebook = notebook,
                Parent = parent,

            };
            sectionGroup.Sections = sectionGroupElement.Elements()
                                                       .Where(s => s.Name == oneNamespace + "Section" || s.Name == oneNamespace + "SectionGroup")
                                                       .Select(s => ParseSectionBase(s, oneNamespace, notebook, sectionGroup));
            return sectionGroup;
        }
        
        private static OneNoteSection ParseSection(XElement sectionElement)
        {
            var section = new OneNoteSection
            {
                ID = sectionElement.Attribute("ID").Value,
                Name = sectionElement.Attribute("name").Value,
                Path = sectionElement.Attribute("path").Value,
                IsUnread = sectionElement.Attribute("isUnread") != null && (bool)sectionElement.Attribute("isUnread"),
                Color = sectionElement.Attribute("color").Value != "none" ? ColorTranslator.FromHtml(sectionElement.Attribute("color").Value) : null,
                Encrypted = sectionElement.Attribute("encrypted") != null && (bool)sectionElement.Attribute("encrypted"),
            };
            return section;
        }
        private static OneNoteSectionExt ParseSection(XElement sectionElement, XNamespace oneNamespace, OneNoteNotebookExt notebook, IOneNoteItem parent)
        {
            var section = new OneNoteSectionExt
            {
                ID = sectionElement.Attribute("ID").Value,
                Name = sectionElement.Attribute("name").Value,
                Path = sectionElement.Attribute("path").Value,
                IsUnread = sectionElement.Attribute("isUnread") != null && (bool)sectionElement.Attribute("isUnread"),
                Color = sectionElement.Attribute("color").Value != "none" ? ColorTranslator.FromHtml(sectionElement.Attribute("color").Value) : null,
                Encrypted = sectionElement.Attribute("encrypted") != null && (bool)sectionElement.Attribute("encrypted"),
                Notebook = notebook,
                Parent = parent
            };
            section.Pages = sectionElement.Elements(oneNamespace + "Page")
                                          .Select(p => ParsePage(p, notebook, section, true));

            return section;
        }

        private static OneNotePage ParsePage(XElement pageElement, OneNoteNotebook notebook, OneNoteSection parent)
        {
            var page = new OneNotePage
            {
                ID = pageElement.Attribute("ID").Value,
                Name = pageElement.Attribute("name").Value,
                Level = (int)pageElement.Attribute("pageLevel"),
                IsUnread = pageElement.Attribute("isUnread") != null && (bool)pageElement.Attribute("isUnread"),
                DateTime = (DateTime)pageElement.Attribute("dateTime"),
                LastModified = (DateTime)pageElement.Attribute("lastModifiedTime"),
                Notebook = notebook,
                Parent = parent,
            };
            return page;
        }
        private static OneNotePageExt ParsePage(XElement pageElement, OneNoteNotebookExt notebook, OneNoteSectionExt parent, bool fullParents)
        {
            var page = new OneNotePageExt
            {
                ID = pageElement.Attribute("ID").Value,
                Name = pageElement.Attribute("name").Value,
                Level = (int)pageElement.Attribute("pageLevel"),
                IsUnread = pageElement.Attribute("isUnread") != null && (bool)pageElement.Attribute("isUnread"),
                DateTime = (DateTime)pageElement.Attribute("dateTime"),
                LastModified = (DateTime)pageElement.Attribute("lastModifiedTime"),
                Notebook = notebook,
                Parent = parent,
            };
            return page;
        }

        private static IEnumerable<OneNotePage> ParsePages(string xml)
        {
            var doc = XElement.Parse(xml);
            var one = doc.GetNamespaceOfPrefix("one");

            // Transform XML into object collection
            foreach (var notebookElement in doc.Elements(one + "Notebook"))
            {
                var notebook = ParseNotebook(notebookElement);
                foreach (var sectionElement in notebookElement.Descendants(one + "Section"))
                {
                    var section = ParseSection(sectionElement);
                    var pages = notebookElement.Descendants(one + "Section").Elements()
                                           .Where(pg => pg.HasAttributes && pg.Name.LocalName == "Page")
                                           .Select(pg => ParsePage(pg, notebook, section));
                    foreach (var pg in pages)
                    {
                        yield return pg;
                    }
                }
            }
            //return pages;

        }

   
        public static void CreatePage(Application oneNote, OneNoteSection section, string pageTitle, bool openImmediately)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsNotebooks, out string oneNoteXMLHierarchy);
            var one = XElement.Parse(oneNoteXMLHierarchy).GetNamespaceOfPrefix("one");

            oneNote.CreateNewPage(section.ID, out string pageID, NewPageStyle.npsBlankPageWithTitle);
            oneNote.GetPageContent(pageID, out string xml, PageInfo.piBasic);

            XDocument doc = XDocument.Parse(xml);
            XElement Xtitle = doc.Descendants(one + "T").First();
            Xtitle.Value = pageTitle;

            oneNote.UpdatePageContent(doc.ToString());

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateQuickNote(Application oneNote, bool openImmediately)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
            oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
            oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateSection(Application oneNote, OneNoteNotebook notebook, string sectionName, bool openImmediately)
        {
            oneNote.OpenHierarchy(sectionName + ".one", notebook.ID, out string sectionID, CreateFileType.cftSection);
            if(openImmediately)
                oneNote.NavigateTo(sectionID);
        }

        public static void CreateNotebook(Application oneNote, string title, bool openImmeditately)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);

            oneNote.OpenHierarchy($"{path}\\{title}", null, out string notebookID, CreateFileType.cftNotebook);
            
            if(openImmeditately)
                oneNote.NavigateTo(notebookID);
        }

        public static string GetDefaultNotebookLocation(Application oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
            return path;
        }
    }
}
