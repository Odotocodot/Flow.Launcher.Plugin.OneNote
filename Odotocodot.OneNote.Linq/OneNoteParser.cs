using Microsoft.Office.Interop.OneNote;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public static class OneNoteParser
    {
        private static string Notebook => nameof(OneNoteItemType.Notebook);
        private static string SectionGroup => nameof(OneNoteItemType.SectionGroup);
        private static string Section => nameof(OneNoteItemType.Section);
        private static string Page => nameof(OneNoteItemType.Page);

        /// <summary>
        /// Hierarchy of notebooks with section groups, sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(Application oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string oneNoteXMLHierarchy);
            var oneNoteHierarchy = XElement.Parse(oneNoteXMLHierarchy);
            var one = oneNoteHierarchy.GetNamespaceOfPrefix("one");

            return oneNoteHierarchy.Elements(one + Notebook)
                                   .Where(n => n.HasAttributes)
                                   .Select(n => ParseNotebook(n, one));
        }

        /// <summary>
        /// Returns a flattened collection of pages.
        /// </summary>
        /// <param name="oneNote"></param>
        public static IEnumerable<OneNotePage> GetPages(Application oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string oneNoteXMLHierarchy);
            return ParsePages(oneNoteXMLHierarchy);
        }

        /// <summary>
        /// Returns a collection of pages that match the specified query term. <br/>
        /// <paramref name="searchString" /> should be exactly the same string that you would type into the search box in the OneNote UI. You can use bitwise operators, such as AND and OR, which must be all uppercase.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <param name="searchString"></param>
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, string searchString)
        {
            return FindPages(oneNote, null, searchString);
        }

        /// <inheritdoc cref="FindPages"/>
        /// <remarks> 
        /// Passing in <paramref name="scope"/> allows for searching within that specific OneNote item. <br/>
        /// If <paramref name="scope" /> is <see langword="null" /> this method is equivalent to <see cref="FindPages(Application,string)"/>
        /// </remarks>
        /// <param name="oneNote"></param>
        /// <param name="scope"></param>
        /// <param name="searchString"></param>
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, IOneNoteItem scope, string searchString)
        {
            oneNote.FindPages(scope?.ID, searchString, out string xml);
            return scope == null 
                ? ParsePages(xml) 
                : ParsePages(xml, scope);
        }

        //public static void DeleteItem(Application oneNote, IOneNoteItem item)
        //{
        //    oneNote.DeleteHierarchy(item.ID);
        //}

        ///// <summary>
        ///// Setting <paramref name="force"/> to <see langword="true"/> to closes the notebook, even if there are changes in the notebook that OneNote cannot sync before closing.
        ///// </summary>
        ///// <param name="oneNote"></param>
        ///// <param name="notebook"></param>
        ///// <param name="force"></param>
        //public static void CloseNotebook(Application oneNote, OneNoteNotebook notebook, bool force = false)
        //{
        //    oneNote.CloseNotebook(notebook.ID, force);
        //}

        //public static IOneNoteItem GetParent(Application oneNote, IOneNoteItem item)
        //{
        //    oneNote.GetHierarchyParent(item.ID, out string parentID);
        //    ///Find Parent with ID
        //}
        #region Parsing XML
        private static OneNoteNotebook ParseNotebook(XElement notebookElement, XNamespace oneNamespace)
        {
            return new OneNoteNotebook
            {
                ID = GetID(notebookElement),
                Name = GetName(notebookElement),
                NickName = notebookElement.Attribute("nickname").Value,
                Path = GetPath(notebookElement),
                IsUnread = GetIsUnread(notebookElement),
                Color = GetColor(notebookElement),
                RelativePath = GetRelativePath(notebookElement, GetName(notebookElement)),
                Sections = notebookElement.Elements()
                                          .Where(s => s.Name.LocalName == Section || s.Name.LocalName == SectionGroup)
                                          .Select(s => ParseSectionBase(s, oneNamespace, GetName(notebookElement))),
            };
        }

        private static IOneNoteItem ParseSectionBase(XElement xElement, XNamespace oneNamespace, string notebookName)
        {
            return xElement.Name.LocalName == Section
                ? ParseSection(xElement, oneNamespace, notebookName)
                : ParseSectionGroup(xElement, oneNamespace, notebookName);
        }

        private static OneNoteSectionGroup ParseSectionGroup(XElement sectionGroupElement, XNamespace oneNamespace, string notebookName)
        {
            return new OneNoteSectionGroup
            {
                ID = GetID(sectionGroupElement),
                Name = GetName(sectionGroupElement),
                Path = GetPath(sectionGroupElement),
                IsUnread = GetIsUnread(sectionGroupElement),
                IsRecycleBin = GetBoolAttribute(sectionGroupElement, "isRecycleBin"),
                RelativePath = GetRelativePath(sectionGroupElement, notebookName),
                Sections = sectionGroupElement.Elements()
                                              .Where(s => s.Name.LocalName == Section || s.Name.LocalName == SectionGroup)
                                              .Select(s => ParseSectionBase(s, oneNamespace, notebookName))
            };
        }

        private static OneNoteSection ParseSection(XElement sectionElement, XNamespace oneNamespace, string notebookName)
        {
            return new OneNoteSection
            {
                ID = GetID(sectionElement),
                Name = GetName(sectionElement),
                Path = GetPath(sectionElement),
                IsUnread = GetIsUnread(sectionElement),
                Color = GetColor(sectionElement),
                IsInRecycleBin = GetIsInRecycleBin(sectionElement),
                IsDeletedPages = GetBoolAttribute(sectionElement, "isDeletedPages"),
                Encrypted = GetBoolAttribute(sectionElement, "encrypted"),
                Locked = GetBoolAttribute(sectionElement, "locked"),
                RelativePath = GetRelativePath(sectionElement, notebookName),
                Pages = sectionElement.Elements(oneNamespace + "Page")
                                      .Select(pg => ParsePage(pg, GetRelativePath(sectionElement, notebookName)))
            };
        }
        
        private static OneNotePage ParsePage(XElement pageElement, string parentRelativePath)
        {
            return new OneNotePage
            {
                ID = GetID(pageElement),
                Name = GetName(pageElement),
                Level = (int)pageElement.Attribute("pageLevel"),
                IsUnread = GetIsUnread(pageElement),
                IsInRecycleBin = GetIsInRecycleBin(pageElement),
                RelativePath = Path.Combine(parentRelativePath[..^4], GetName(pageElement)),
                Created = (DateTime)pageElement.Attribute("dateTime"),
                LastModified = (DateTime)pageElement.Attribute("lastModifiedTime"),
            };
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
            while (notebookElement.Name.LocalName == SectionGroup)
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

            return doc.Elements(one + Notebook)
                      .Descendants(one + Section)
                      .Elements(one + Page)
                      //.Elements()
                      .Where(x => x.HasAttributes)// && x.Name.LocalName == "Page")
                      .Select(pg => ParsePage(pg));
        }

        private static IEnumerable<OneNotePage> ParsePages(string xml, IOneNoteItem scope)
        {
            var doc = XElement.Parse(xml);
            var one = doc.GetNamespaceOfPrefix("one");

            if (scope.ItemType == OneNoteItemType.Section)
            {
                return doc.Elements(one + Page)
                          .Select(pg => ParsePage(pg, scope.RelativePath));
            }
            else
            {
                return doc.Descendants(one + Section)
                          .Elements(one + Page)
                          .Select(pg => ParsePage(pg, scope));
            }
        }
        #endregion

        #region XElement Helpers
        private static string GetID(XElement element) => element.Attribute("ID").Value;
        private static string GetName(XElement element) => element.Attribute("name").Value;
        private static string GetPath(XElement element) => element.Attribute("path").Value;
        private static bool GetIsUnread(XElement element) => GetBoolAttribute(element, "isUnread");
        private static bool GetIsInRecycleBin(XElement element) => GetBoolAttribute(element, "isInRecycleBin");
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
        private static bool GetBoolAttribute(XElement element, string name)
        {
            var attr = element.Attribute(name);
            return attr != null && (bool)attr;
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
            XElement xTitle = doc.Descendants(one + "T").First();
            xTitle.Value = pageTitle;

            oneNote.UpdatePageContent(doc.ToString());

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateQuickNote(Application oneNote, bool openImmediately)
        {
            var path = GetUnfiledNotesSection(oneNote);
            oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
            oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateSection(Application oneNote, IOneNoteItem parent, string sectionName, bool openImmediately)
        {
            if (parent.ItemType == OneNoteItemType.Page || parent.ItemType == OneNoteItemType.Section)
                throw new ArgumentException("The parent item type must a notebook or section group");

            ArgumentException.ThrowIfNullOrEmpty(sectionName, nameof(sectionName));


            oneNote.OpenHierarchy(sectionName + ".one", parent.ID, out string sectionID, CreateFileType.cftSection);
            if(openImmediately)
                oneNote.NavigateTo(sectionID);
        }
        public static void CreateSectionGroup(Application oneNote, IOneNoteItem parent, string sectionGroupName, bool openImmediately)
        {
            if (parent.ItemType == OneNoteItemType.Page || parent.ItemType == OneNoteItemType.Section)
                throw new ArgumentException("The parent item type must a notebook or section group", nameof(parent));

            ArgumentException.ThrowIfNullOrEmpty(sectionGroupName, nameof(sectionGroupName));

            oneNote.OpenHierarchy(sectionGroupName, parent.ID, out string sectionGroupID, CreateFileType.cftFolder);
            if (openImmediately)
                oneNote.NavigateTo(sectionGroupID);
        }
        public static void CreateNotebook(Application oneNote, string title, bool openImmediately)
        {
            ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));

            var path = GetDefaultNotebookLocation(oneNote);

            oneNote.OpenHierarchy($"{path}\\{title}", null, out string notebookID, CreateFileType.cftNotebook);
            
            if(openImmediately)
                oneNote.NavigateTo(notebookID);
        }
        #endregion

        #region Special Folder Locations
        /// <summary>
        /// Returns the path to the default notebook folder location, this is where new notebooks are created and saved to.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetDefaultNotebookLocation(Application oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
            return path;
        }
        /// <summary>
        /// Returns the path to the back up folder location.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetBackUpLocation(Application oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slBackUpFolder, out string path);
            return path;
        }
        /// <summary>
        /// Returns the folder path of the unfiled notes section, this is also where quick notes are created and saved to.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetUnfiledNotesSection(Application oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
            return path;
        }
        #endregion
    }
}
