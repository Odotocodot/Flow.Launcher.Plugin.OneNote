using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public static class OneNoteParser
    {
        /// <summary>
        /// Hierarchy of notebooks with section groups, sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(Application oneNote)
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
        /// Collection of pages.
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
            var pages = ParsePages(xml);
            return pages;
        }
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, IOneNoteItem scope, string searchString)
        {
            oneNote.FindPages(scope?.ID, searchString, out string xml);
            var pages = ParsePages(xml, scope);
            return pages;
        }

        #region Parsing XML
        private static OneNoteNotebook ParseNotebook(XElement notebookElement, XNamespace oneNamespace)
        {
            var notebook = new OneNoteNotebook
            {
                ID = GetID(notebookElement),
                Name = GetName(notebookElement),
                NickName = notebookElement.Attribute("nickname").Value,
                Path = GetPath(notebookElement),
                IsUnread = GetIsUnread(notebookElement),
                Color = GetColor(notebookElement),
                RelativePath = GetRelativePath(notebookElement, GetName(notebookElement)),
                Sections = notebookElement.Elements()
                                          .Where(s => s.Name.LocalName == "Section" || s.Name.LocalName == "SectionGroup")
                                          .Select(s => ParseSectionBase(s, oneNamespace, GetName(notebookElement))),
            };
            return notebook;
        }

        private static IOneNoteItem ParseSectionBase(XElement xElement, XNamespace oneNamespace, string notebookName)
        {
            if (xElement.Name.LocalName == "Section")
                return ParseSection(xElement, oneNamespace, notebookName);
            else
                return ParseSectionGroup(xElement, oneNamespace, notebookName);
        }

        private static OneNoteSectionGroup ParseSectionGroup(XElement sectionGroupElement, XNamespace oneNamespace, string notebookName)
        {
            var sectionGroup = new OneNoteSectionGroup
            {
                ID = GetID(sectionGroupElement),
                Name = GetName(sectionGroupElement),
                Path = GetPath(sectionGroupElement),
                IsUnread = GetIsUnread(sectionGroupElement),
                RelativePath = GetRelativePath(sectionGroupElement, notebookName),
                Sections = sectionGroupElement.Elements()
                                              .Where(s => s.Name == oneNamespace + "Section" || s.Name == oneNamespace + "SectionGroup")
                                              .Select(s => ParseSectionBase(s, oneNamespace, notebookName))
            };
            return sectionGroup;
        }

        private static OneNoteSection ParseSection(XElement sectionElement, XNamespace oneNamespace, string notebookName)
        {
            var section = new OneNoteSection
            {
                ID = GetID(sectionElement),
                Name = GetName(sectionElement),
                Path = GetPath(sectionElement),
                IsUnread = GetIsUnread(sectionElement),
                Color = GetColor(sectionElement),
                Encrypted = sectionElement.Attribute("encrypted") != null && (bool)sectionElement.Attribute("encrypted"),
                RelativePath = GetRelativePath(sectionElement, notebookName),
                Pages = sectionElement.Elements(oneNamespace + "Page")
                                      .Select(pg => ParsePage(pg, GetRelativePath(sectionElement, notebookName)))
            };
            return section;
        }
        
        private static OneNotePage ParsePage(XElement pageElement, string parentRelativePath)
        {
            
            var page = new OneNotePage
            {
                ID = GetID(pageElement),
                Name = GetName(pageElement),
                Level = (int)pageElement.Attribute("pageLevel"),
                IsUnread = GetIsUnread(pageElement),
                RelativePath = Path.Combine(parentRelativePath[..^4], GetName(pageElement)),
                DateTime = (DateTime)pageElement.Attribute("dateTime"),
                LastModified = (DateTime)pageElement.Attribute("lastModifiedTime"),
            };
            return page;
        }
        private static OneNotePage ParsePage(XElement pageElement, IOneNoteItem scope)
        {
            var sectionElement = pageElement.Parent;
            var notebookName = scope.RelativePath.Split(new char[] { '/', '\\' }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
            var sectionRelativePath = GetRelativePath(sectionElement, notebookName);
            return ParsePage(pageElement, sectionRelativePath);
        }

        private static OneNotePage ParsePage(XElement pageElement)
        {
            var sectionElement = pageElement.Parent;
            var notebookElement = sectionElement.Parent;
            while (notebookElement.Name.LocalName == "SectionGroup")
            {
                notebookElement = notebookElement.Parent;
            }
            var notebookName = GetName(notebookElement);
            var sectionRelativePath = GetRelativePath(sectionElement, notebookName);
            return ParsePage(pageElement, sectionRelativePath);
        }


        private static IEnumerable<OneNotePage> ParsePages(string xml)
        {
            var doc = XElement.Parse(xml);
            var one = doc.GetNamespaceOfPrefix("one");

            return doc.Elements(one + "Notebook")
                        .Descendants(one + "Section")
                        //.Elements(one + "Page")
                        .Elements()
                        .Where(x => x.HasAttributes && x.Name.LocalName == "Page")
                        .Select(pg => ParsePage(pg));
        }

        private static IEnumerable<OneNotePage> ParsePages(string xml, IOneNoteItem scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var doc = XElement.Parse(xml);
            var one = doc.GetNamespaceOfPrefix("one");

            if (scope.ItemType == OneNoteItemType.Section)
            {
                return doc.Elements(one + "Page")
                          .Select(pg => ParsePage(pg, scope.RelativePath));
            }
            else
            {
                return doc.Descendants(one + "Section")
                          .Elements(one + "Page")
                          .Select(pg => ParsePage(pg, scope));
            }
        }
        #endregion

        #region XElement Helpers
        private static string GetID(XElement element) => element.Attribute("ID").Value;
        private static string GetName(XElement element) => element.Attribute("name").Value;
        private static string GetPath(XElement element) => element.Attribute("path").Value;
        private static bool GetIsUnread(XElement element)
        {
            var isUnread = element.Attribute("isUnread");
            return isUnread != null && (bool)isUnread;
        }
        private static Color? GetColor(XElement element)
        {
            string color = element.Attribute("color").Value;
            return color != "none" ? ColorTranslator.FromHtml(color) : null;
        }
        private static string GetRelativePath(XElement element, string notebookName)
        {
            var path = GetPath(element);
            return path[path.IndexOf(notebookName)..];
        }
        #endregion

        #region Creating OneNote Items
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
        public static void CreateSection(Application oneNote, IOneNoteItem parent, string sectionName, bool openImmediately)
        {
            if (parent.ItemType == OneNoteItemType.Page || parent.ItemType == OneNoteItemType.Section)
                throw new ArgumentException("The parent item type must a notebook or section group");

            oneNote.OpenHierarchy(sectionName + ".one", parent.ID, out string sectionID, CreateFileType.cftSection);
            if(openImmediately)
                oneNote.NavigateTo(sectionID);
        }
        public static void CreateSectionGroup(Application oneNote, IOneNoteItem parent, string sectionGroupName, bool openImmediately)
        {
            if (parent.ItemType == OneNoteItemType.Page || parent.ItemType == OneNoteItemType.Section)
                throw new ArgumentException("The parent item type must a notebook or section group");

            oneNote.OpenHierarchy(sectionGroupName, parent.ID, out string sectionGroupID, CreateFileType.cftFolder);
            if (openImmediately)
                oneNote.NavigateTo(sectionGroupID);
        }
        public static void CreateNotebook(Application oneNote, string title, bool openImmeditately)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);

            oneNote.OpenHierarchy($"{path}\\{title}", null, out string notebookID, CreateFileType.cftNotebook);
            
            if(openImmeditately)
                oneNote.NavigateTo(notebookID);
        }
        #endregion

        public static string GetDefaultNotebookLocation(Application oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
            return path;
        }
    }
}
