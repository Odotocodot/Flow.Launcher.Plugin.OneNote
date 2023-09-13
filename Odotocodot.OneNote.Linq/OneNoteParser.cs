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
        private const string NamespacePrefix = "one";
        public const string RelativePathSeparator = "\\";

        public const string InvalidNotebookCharacters = """\/*?"|<>:%#.""";
        public const string InvalidSectionCharacters = """\/*?"|<>:%#&""";
        public const string InvalidSectionGroupCharacters = InvalidSectionCharacters;

        private static readonly Lazy<char[]> invalidNotebookCharacters = new(InvalidNotebookCharacters.ToCharArray);
        private static readonly Lazy<char[]> invalidSectionCharacters = new(InvalidSectionCharacters.ToCharArray);
        private static readonly Lazy<char[]> invalidSectionGroupCharacters = invalidSectionCharacters;

        private const string NamespaceUri = "http://schemas.microsoft.com/office/onenote/2013/onenote";
        private static readonly XName NotebookXName = XName.Get("Notebook", NamespaceUri);
        private static readonly XName SectionGroupXName = XName.Get("SectionGroup", NamespaceUri);
        private static readonly XName SectionXName = XName.Get("Section", NamespaceUri);
        private static readonly XName PageXName = XName.Get("Page", NamespaceUri);
        private static OneNotePage ParsePage(XElement element, OneNoteSection parent)
        {
            var page = new OneNotePage();
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "ID":
                        page.ID = attribute.Value;
                        break;
                    case "name":
                        page.Name = attribute.Value;
                        break;
                    case "dateTime":
                        page.Created = (DateTime)attribute;
                        break;
                    case "lastModifiedTime":
                        page.LastModified = (DateTime)attribute;
                        break;
                    case "pageLevel":
                        page.Level = (int)attribute;
                        break;
                    case "isUnread":
                        page.IsUnread = (bool)attribute;
                        break;
                    case "isInRecycleBin":
                        page.IsInRecycleBin = (bool)attribute;
                        break;
                }
            }
            page.Section = parent;
            page.Notebook = parent.Notebook;
            page.RelativePath = $"{parent.RelativePath}{RelativePathSeparator}{page.Name}";
            return page;
        }

        private static OneNoteSection ParseSection(XElement element, IOneNoteItem parent)
        {
            var section = new OneNoteSection();
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "name":
                        section.Name = attribute.Value;
                        break;
                    case "ID":
                        section.ID = attribute.Value;
                        break;
                    case "path":
                        section.Path = attribute.Value;
                        break;
                    case "isUnread":
                        section.IsUnread = (bool)attribute;
                        break;
                    case "color":
                        section.Color = attribute.Value != "none" ? ColorTranslator.FromHtml(attribute.Value) : null;
                        break;
                    case "lastModifiedTime":
                        section.LastModified = (DateTime)attribute;
                        break;
                    case "encrypted":
                        section.Encrypted = (bool)attribute;
                        break;
                    case "locked":
                        section.Locked = (bool)attribute;
                        break;
                    case "isInRecycleBin":
                        section.IsInRecycleBin = (bool)attribute;
                        break;
                    case "isDeletedPages":
                        section.IsDeletedPages = (bool)attribute;
                        break;
                }
            }
            section.Parent = parent;
            section.Notebook = parent.Notebook;
            section.RelativePath = $"{parent.RelativePath}{RelativePathSeparator}{section.Name}";
            section.Pages = element.Elements(PageXName)
                                   .Select(e => ParsePage(e, section));
            return section;
        }

        private static OneNoteSectionGroup ParseSectionGroup(XElement element, IOneNoteItem parent)
        {
            var sectionGroup = new OneNoteSectionGroup();
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "name":
                        sectionGroup.Name = attribute.Value;
                        break;
                    case "ID":
                        sectionGroup.ID = attribute.Value;
                        break;
                    case "path":
                        sectionGroup.Path = attribute.Value;
                        break;
                    case "lastModifiedTime":
                        sectionGroup.LastModified = (DateTime)attribute;
                        break;
                    case "isUnread":
                        sectionGroup.IsUnread = (bool)attribute;
                        break;
                    case "isRecycleBin":
                        sectionGroup.IsRecycleBin = (bool)attribute;
                        break;
                }
            }
            sectionGroup.Notebook = parent.Notebook;
            sectionGroup.Parent = parent;
            sectionGroup.RelativePath = $"{parent.RelativePath}{RelativePathSeparator}{sectionGroup.Name}";
            sectionGroup.Sections = element.Elements(SectionXName)
                                           .Select(e => ParseSection(e, sectionGroup));
            sectionGroup.SectionGroups = element.Elements(SectionGroupXName)
                                                .Select(e => ParseSectionGroup(e, sectionGroup));
            return sectionGroup;

        }

        private static OneNoteNotebook ParseNotebook(XElement element)
        {
            var notebook = new OneNoteNotebook();
            //Technically 'faster' than the XElement.GetAttribute method
            foreach (var attribute in element.Attributes())
            {
                switch (attribute.Name.LocalName)
                {
                    case "name":
                        notebook.Name = attribute.Value;
                        break;
                    case "nickname":
                        notebook.NickName = attribute.Value;
                        break;
                    case "ID":
                        notebook.ID = attribute.Value;
                        break;
                    case "path":
                        notebook.Path = attribute.Value;
                        break;
                    case "lastModifiedTime":
                        notebook.LastModified = (DateTime)attribute;
                        break;
                    case "color":
                        notebook.Color = attribute.Value != "none" ? ColorTranslator.FromHtml(attribute.Value) : null;
                        break;
                    case "isUnread":
                        notebook.IsUnread = (bool)attribute;
                        break;
                }
            }
            notebook.Notebook = notebook;
            notebook.Sections = element.Elements(SectionXName)
                                       .Select(e => ParseSection(e, notebook));
            notebook.SectionGroups = element.Elements(SectionGroupXName)
                                            .Select(e => ParseSectionGroup(e, notebook));
            return notebook;
        }
        /// <summary>
        /// Hierarchy of notebooks with section groups, sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(IApplication oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(NotebookXName)
                              .Select(ParseNotebook);
        }

        /// <summary>
        /// Returns a collection of pages that match the specified query term. <br/>
        /// <paramref name="search" /> should be exactly the same string that you would type into the search box in the OneNote UI. You can use bitwise operators, such as AND and OR, which must be all uppercase.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <param name="search"></param>
        public static IEnumerable<OneNotePage> FindPages(IApplication oneNote, string search)
        {
            ValidateSearch(search);

            oneNote.FindPages(null, search, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(NotebookXName)
                              .Select(ParseNotebook)
                              .GetPages();
        }

        /// <inheritdoc cref="FindPages"/>
        /// <remarks> 
        /// Passing in <paramref name="scope"/> allows for searching within that specific OneNote item. <br/>
        /// If <paramref name="scope" /> is <see langword="null" /> this method is equivalent to <see cref="FindPages(IApplication,string)"/>
        /// </remarks>
        /// <param name="oneNote"></param>
        /// <param name="search"></param>
        /// <param name="scope"></param>
        public static IEnumerable<OneNotePage> FindPages(IApplication oneNote, string search, IOneNoteItem scope)
        {
            ArgumentNullException.ThrowIfNull(scope, nameof(scope));

            ValidateSearch(search);

            oneNote.FindPages(scope.ID, search, out string xml);

            var rootElement = XElement.Parse(xml);

            IOneNoteItem root = scope switch
            {
                OneNoteNotebook => ParseNotebook(rootElement),
                OneNoteSectionGroup => ParseSectionGroup(rootElement, scope.Parent),
                OneNoteSection => ParseSection(rootElement, scope.Parent),
                OneNotePage => ParsePage(rootElement, (OneNoteSection)scope.Parent),
                _ => null,
            };
            return root.GetPages();
        }

        private static void ValidateSearch(string search)
        {
            ArgumentNullException.ThrowIfNull(search, nameof(search));

            if (string.IsNullOrWhiteSpace(search))
                throw new ArgumentException("Search string cannot be empty or only whitespace", nameof(search));

            if (!char.IsLetterOrDigit(search[0]))
                throw new ArgumentException("The first character of the search must be a letter or a digit", nameof(search));
        }

        public static void OpenInOneNote(IApplication oneNote, IOneNoteItem item)
        {
            oneNote.NavigateTo(item.ID);
        }
        public static void SyncItem(IApplication oneNote, IOneNoteItem item)
        {
            oneNote.SyncHierarchy(item.ID);
        }

        public static string GetPageContent(Application oneNote, OneNotePage page)
        {
            oneNote.GetPageContent(page.ID, out string xml);
            return xml;
        }

        public static void DeleteItem(IApplication oneNote, IOneNoteItem item)
        {
            oneNote.DeleteHierarchy(item.ID);
        }

        public static void CloseNotebook(IApplication oneNote, OneNoteNotebook notebook)
        {
            oneNote.CloseNotebook(notebook.ID);
        }

        #region Creating New OneNote Items
        public static void CreatePage(IApplication oneNote, OneNoteSection section, string pageTitle, bool openImmediately)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsNotebooks, out string oneNoteXMLHierarchy);
            var one = XElement.Parse(oneNoteXMLHierarchy).GetNamespaceOfPrefix(NamespacePrefix);

            oneNote.CreateNewPage(section.ID, out string pageID, NewPageStyle.npsBlankPageWithTitle);
            oneNote.GetPageContent(pageID, out string xml, PageInfo.piBasic);

            XDocument doc = XDocument.Parse(xml);
            XElement xTitle = doc.Descendants(one + "T").First();
            xTitle.Value = pageTitle;

            oneNote.UpdatePageContent(doc.ToString());

            if (openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateQuickNote(IApplication oneNote, bool openImmediately)
        {
            var path = GetUnfiledNotesSection(oneNote);
            oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
            oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);

            if (openImmediately)
                oneNote.NavigateTo(pageID);
        }

        private static void CreateItemBase<T>(IApplication oneNote, IOneNoteItem parent, string title, bool openImmediately) where T : IOneNoteItem
        {
            ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));

            string path = string.Empty;
            CreateFileType createFileType = CreateFileType.cftNone;
            switch (typeof(T).Name) //kinda smelly
            {
                case nameof(OneNoteNotebook):
                    if (!IsNotebookTitleValid(title))
                        throw new ArgumentException($"Invalid notebook name. Notebook names cannot contain the symbols: \n {string.Join(' ', InvalidNotebookCharacters.ToCharArray())}");

                    path = Path.Combine(GetDefaultNotebookLocation(oneNote), title);
                    createFileType = CreateFileType.cftNotebook;
                    break;
                case nameof(OneNoteSectionGroup):
                    if (!IsSectionGroupTitleValid(title))
                        throw new ArgumentException($"Invalid section group name. Section groups names cannot contain the symbols: \n {string.Join(' ', InvalidSectionGroupCharacters.ToCharArray())}");

                    path = title;
                    createFileType = CreateFileType.cftFolder;

                    break;
                case nameof(OneNoteSection):
                    if (!IsSectionTitleValid(title))
                        throw new ArgumentException($"Invalid section name. Section names cannot contain the symbols: \n {string.Join(' ', InvalidSectionCharacters.ToCharArray())}");

                    path = title + ".one";
                    createFileType = CreateFileType.cftSection;
                    break;
            }

            oneNote.OpenHierarchy(path, parent?.ID, out string newItemID, createFileType);

            if (openImmediately)
                oneNote.NavigateTo(newItemID);

        }

        public static void CreateSection(IApplication oneNote, OneNoteSectionGroup parent, string sectionName, bool openImmediately)
        {
            CreateItemBase<OneNoteSection>(oneNote, parent, sectionName, openImmediately);
        }
        public static void CreateSection(IApplication oneNote, OneNoteNotebook parent, string sectionName, bool openImmediately)
        {
            CreateItemBase<OneNoteSection>(oneNote, parent, sectionName, openImmediately);
        }
        public static void CreateSectionGroup(IApplication oneNote, OneNoteSectionGroup parent, string sectionGroupName, bool openImmediately)
        {
            CreateItemBase<OneNoteSectionGroup>(oneNote, parent, sectionGroupName, openImmediately);
        }
        public static void CreateSectionGroup(IApplication oneNote, OneNoteNotebook parent, string sectionGroupName, bool openImmediately)
        {
            CreateItemBase<OneNoteSectionGroup>(oneNote, parent, sectionGroupName, openImmediately);
        }
        public static void CreateNotebook(IApplication oneNote, string notebookName, bool openImmediately)
        {
            CreateItemBase<OneNoteNotebook>(oneNote, null, notebookName, openImmediately);
        }
        public static bool IsNotebookTitleValid(string notebookTitle)
        {
            return notebookTitle.IndexOfAny(invalidNotebookCharacters.Value) == -1;
        }
        public static bool IsSectionTitleValid(string sectionTitle)
        {
            return sectionTitle.IndexOfAny(invalidSectionCharacters.Value) == -1;
        }
        public static bool IsSectionGroupTitleValid(string sectionGroupTitle)
        {
            return sectionGroupTitle.IndexOfAny(invalidSectionGroupCharacters.Value) == -1;
        }

        #endregion

        #region Special Folder Locations
        /// <summary>
        /// Returns the path to the default notebook folder location, this is where new notebooks are created and saved to.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetDefaultNotebookLocation(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
            return path;
        }
        /// <summary>
        /// Returns the path to the back up folder location.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetBackUpLocation(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slBackUpFolder, out string path);
            return path;
        }
        /// <summary>
        /// Returns the folder path of the unfiled notes section, this is also where quick notes are created and saved to.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <returns></returns>
        public static string GetUnfiledNotesSection(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
            return path;
        }

        #endregion
    }
}
