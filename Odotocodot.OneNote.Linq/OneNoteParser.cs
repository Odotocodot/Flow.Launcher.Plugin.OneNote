using Microsoft.Office.Interop.OneNote;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// A static helper class responsible for deserializing and parsing OneNote's XML output and exposing <see href="https://learn.microsoft.com/en-us/office/client-developer/onenote/application-interface-onenote">OneNote's API</see>.
    /// </summary>
    public static class OneNoteParser
    {
        /// <summary>
        /// The directory separator used in <see cref="IOneNoteItem.RelativePath"/>.
        /// </summary>
        public const char RelativePathSeparator = '\\';

        /// <summary>
        /// An array containing the characters that are not allowed in a <see cref="OneNoteNotebook">notebook</see> <see cref="OneNoteNotebook.Name">name</see>.<br/>
        /// These are:&#009;<b>\ / * ? " | &lt; &gt; : % # .</b>
        /// </summary>
        /// <seealso cref="IsNotebookNameValid(string)"/>
        /// <seealso cref="InvalidSectionChars"/>
        /// <seealso cref="InvalidSectionGroupChars"/>
        public static readonly ImmutableArray<char> InvalidNotebookChars = """\/*?"|<>:%#.""".ToImmutableArray();
        /// <summary>
        /// An array containing the characters that are not allowed in a <see cref="OneNoteSection">section</see> <see cref="OneNoteSection.Name">name</see>.<br/>
        /// These are:&#009;<b>\ / * ? " | &lt; &gt; : % # &amp;</b>
        /// </summary>
        /// <seealso cref="IsSectionNameValid(string)"/>
        /// <seealso cref="InvalidNotebookChars"/>
        /// <seealso cref="InvalidSectionGroupChars"/>
        public static readonly ImmutableArray<char> InvalidSectionChars = """\/*?"|<>:%#&""".ToImmutableArray();
        /// <summary>
        /// An array containing the characters that are not allowed in a <see cref="OneNoteSectionGroup">section group</see> <see cref="OneNoteSectionGroup.Name">name</see>.<br/>
        /// These are:&#009;<b>\ / * ? " | &lt; &gt; : % # &amp;</b>
        /// </summary>
        /// <seealso cref="IsSectionGroupNameValid(string)"/>
        /// <seealso cref="InvalidNotebookChars"/>
        /// <seealso cref="InvalidSectionChars"/>
        public static readonly ImmutableArray<char> InvalidSectionGroupChars = InvalidSectionChars;

        private const string NamespacePrefix = "one";

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
        /// Get all notebooks down to all children.
        /// </summary>
        /// <param name="oneNote">The OneNote Com object to be used.</param>
        /// <returns>The full hierarchy node structure with <see cref="IEnumerable{T}">IEnumerable</see>&lt;<see cref="OneNoteNotebook"/>&gt; as the root.</returns>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(IApplication oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(NotebookXName)
                              .Select(ParseNotebook);
        }

        /// <summary>
        /// Get a flattened collection of <see cref="OneNotePage">pages</see> that match the <paramref name="search"/> parameter.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="search">The search query. This should be exactly the same string that you would type into the search box in the OneNote UI. You can use bitwise operators, such as AND and OR, which must be all uppercase.</param>
        /// <returns>A <see cref="IEnumerable{T}">IEnumerable</see>&lt;<see cref="OneNotePage"/>&gt; that contains <see cref="OneNotePage">pages</see> that match the <paramref name="search"/> parameter.</returns>
        /// <inheritdoc cref="ValidateSearch(string)" path="/exception"/>
        /// <seealso cref="FindPages(IApplication, string, IOneNoteItem)"/>
        public static IEnumerable<OneNotePage> FindPages(IApplication oneNote, string search)
        {
            ValidateSearch(search);

            oneNote.FindPages(null, search, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(NotebookXName)
                              .Select(ParseNotebook)
                              .GetPages();
        }

        /// <summary>
        /// <inheritdoc cref="FindPages(IApplication, string)" path="/summary"/> Within the specified <paramref name="scope"/>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="search"><inheritdoc cref="FindPages(IApplication, string)" path="/param[@name='search']"/></param>
        /// <param name="scope">The hierarchy item to search within.</param>
        /// <returns><inheritdoc cref="FindPages(IApplication, string)" path="/returns"/></returns>
        /// <seealso cref="FindPages(IApplication, string)"/>
        /// <exception cref="ArgumentException"><inheritdoc cref="ValidateSearch(string)" path="/param[@cref='ArgumentException'"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="search"/> or <paramref name="scope"/> is <see langword="null"/>.</exception>
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

        //TODO: Open FindByID

        /// <summary>
        /// 
        /// </summary>
        /// <param name="search"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="search"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="search"/> is empty or only whitespace, or if the first character of <paramref name="search"/> is NOT a letter or a digit.</exception>
        private static void ValidateSearch(string search)
        {
            ArgumentNullException.ThrowIfNull(search, nameof(search));

            if (string.IsNullOrWhiteSpace(search))
                throw new ArgumentException("Search string cannot be empty or only whitespace", nameof(search));

            if (!char.IsLetterOrDigit(search[0]))
                throw new ArgumentException("The first character of the search must be a letter or a digit", nameof(search));
        }

        /// <summary>
        /// Opens the <paramref name="item"/> in OneNote (creates a new OneNote window if one is not currently open).
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="item">The OneNote hierarchy item.</param>
        public static void OpenInOneNote(IApplication oneNote, IOneNoteItem item) => oneNote.NavigateTo(item.ID);

        /// <summary>
        /// Forces OneNote to sync the <paramref name="item"/>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="item"><inheritdoc cref="OpenInOneNote(IApplication, IOneNoteItem)" path="/param[@name='item']"/></param>
        public static void SyncItem(IApplication oneNote, IOneNoteItem item) => oneNote.SyncHierarchy(item.ID);

        /// <summary>
        /// Gets the content of the specified <paramref name="page"/>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="page">The page to retrieve content from.</param>
        /// <returns>A <see langword="string"/> in the OneNote XML format.</returns>
        public static string GetPageContent(IApplication oneNote, OneNotePage page)
        {
            oneNote.GetPageContent(page.ID, out string xml);
            return xml;
        }

        /// <summary>
        /// Deletes the hierarchy <paramref name="item"/> from the OneNote notebook hierarchy.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="item"><inheritdoc cref="OpenInOneNote(IApplication, IOneNoteItem)" path="/param[@name='item']"/></param>
        public static void DeleteItem(IApplication oneNote, IOneNoteItem item) => oneNote.DeleteHierarchy(item.ID);

        /// <summary>
        /// Closes the <paramref name="notebook"/>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="notebook">The specified OneNote notebook.</param>
        public static void CloseNotebook(IApplication oneNote, OneNoteNotebook notebook) => oneNote.CloseNotebook(notebook.ID);

        //TODO: Rename item

        #region Creating New OneNote Items
        //TODO: change to return ID

        /// <summary>
        /// Creates a <see cref="OneNotePage">page</see> with a title equal to <paramref name="pageTitle"/> located in the specified <paramref name="section"/>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="section">The section to create the page in.</param>
        /// <param name="pageTitle">The title of the page.</param>
        /// <param name="openImmediately">Whether to open the newly created page in OneNote immediately.</param>
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

        /// <summary>
        /// Creates a quick note page located currently set quick notes location.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="openImmediately"><inheritdoc cref="CreatePage(IApplication, OneNoteSection, string, bool)" path="/param[@name='openImmediately']"/></param>
        public static void CreateQuickNote(IApplication oneNote, bool openImmediately)
        {
            var path = GetUnfiledNotesSection(oneNote);
            oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
            oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);

            if (openImmediately)
                oneNote.NavigateTo(pageID);
        }

        private static void CreateItemBase<T>(IApplication oneNote, IOneNoteItem parent, string name, bool openImmediately) where T : IOneNoteItem
        {
            string path = string.Empty;
            CreateFileType createFileType = CreateFileType.cftNone;
            switch (typeof(T).Name) //kinda smelly
            {
                case nameof(OneNoteNotebook):
                    if (!IsNotebookNameValid(name))
                        throw new ArgumentException($"Invalid notebook name provided: \"{name}\". Notebook names cannot empty, only white space or contain the symbols: \n {string.Join(' ', InvalidNotebookChars)}");

                    path = Path.Combine(GetDefaultNotebookLocation(oneNote), name);
                    createFileType = CreateFileType.cftNotebook;
                    break;
                case nameof(OneNoteSectionGroup):
                    if (!IsSectionGroupNameValid(name))
                        throw new ArgumentException($"Invalid section group name provided: \"{name}\". Section group names cannot empty, only white space or contain the symbols: \n {string.Join(' ', InvalidSectionGroupChars)}");

                    path = name;
                    createFileType = CreateFileType.cftFolder;

                    break;
                case nameof(OneNoteSection):
                    if (!IsSectionNameValid(name))
                        throw new ArgumentException($"Invalid section name provided: \"{name}\". Section names cannot empty, only white space or contain the symbols: \n {string.Join(' ', InvalidSectionChars)}");

                    path = name + ".one";
                    createFileType = CreateFileType.cftSection;
                    break;
            }

            oneNote.OpenHierarchy(path, parent?.ID, out string newItemID, createFileType);

            if (openImmediately)
                oneNote.NavigateTo(newItemID);
        }

        /// <summary>
        /// Creates a <see cref="OneNoteSection">section</see> with a title equal to <paramref name="name"/> located in the specified <paramref name="parent"/> 
        /// <see cref="OneNoteSectionGroup">section group</see>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="parent">The hierarchy item to create the section in.</param>
        /// <param name="name">The proposed name of the new section.</param>
        /// <param name="openImmediately">Whether to open the newly created section in OneNote immediately.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is not a valid section name.</exception>
        /// <seealso cref="IsSectionNameValid(string)"/>
        public static void CreateSection(IApplication oneNote, OneNoteSectionGroup parent, string name, bool openImmediately) 
            => CreateItemBase<OneNoteSection>(oneNote, parent, name, openImmediately);

        /// <summary>
        /// Creates a <see cref="OneNoteSection">section</see> with a title equal to <paramref name="name"/> located in the specified <paramref name="parent"/> 
        /// <see cref="OneNoteNotebook">notebook</see>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="parent">The hierarchy item to create the section in.</param>
        /// <param name="name">The proposed name of the new section.</param>
        /// <param name="openImmediately">Whether to open the newly created section in OneNote immediately.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is not a valid section name.</exception>
        /// <seealso cref="IsSectionNameValid(string)"/>
        public static void CreateSection(IApplication oneNote, OneNoteNotebook parent, string name, bool openImmediately) 
            => CreateItemBase<OneNoteSection>(oneNote, parent, name, openImmediately);

        /// <summary>
        /// Creates a <see cref="OneNoteSectionGroup">section group</see> with a title equal to <paramref name="name"/> located in the specified <paramref name="parent"/> 
        /// <see cref="OneNoteSectionGroup">section group</see>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="parent">The hierarchy item to create the section group in.</param>
        /// <param name="name">The proposed name of the new section group.</param>
        /// <param name="openImmediately">Whether to open the newly created section group in OneNote immediately.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is not a valid section group name.</exception>
        /// <seealso cref="IsSectionGroupNameValid(string)"/>
        public static void CreateSectionGroup(IApplication oneNote, OneNoteSectionGroup parent, string name, bool openImmediately) 
            => CreateItemBase<OneNoteSectionGroup>(oneNote, parent, name, openImmediately);

        /// <summary>
        /// Creates a <see cref="OneNoteSectionGroup">section group</see> with a title equal to <paramref name="name"/> located in the specified <paramref name="parent"/> 
        /// <see cref="OneNoteNotebook">notebook</see>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="parent">The hierarchy item to create the section group in.</param>
        /// <param name="name">The proposed name of the new section group.</param>
        /// <param name="openImmediately">Whether to open the newly created section group in OneNote immediately.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is not a valid section group name.</exception>
        public static void CreateSectionGroup(IApplication oneNote, OneNoteNotebook parent, string name, bool openImmediately)
            => CreateItemBase<OneNoteSectionGroup>(oneNote, parent, name, openImmediately);


        /// <summary>
        /// Creates a <see cref="OneNoteNotebook">notebook</see> with a title equal to <paramref name="name"/> located in the <see cref="GetDefaultNotebookLocation(IApplication)">defualt notebook location</see>.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <param name="name">The proposed name of the new notebook.</param>
        /// <param name="openImmediately">Whether to open the newly created notebook in OneNote immediately.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="name"/> is not a valid notebook name.</exception>
        public static void CreateNotebook(IApplication oneNote, string name, bool openImmediately)
            => CreateItemBase<OneNoteNotebook>(oneNote, null, name, openImmediately);

        /// <summary>
        /// Returns a value that indicates whether the supplied <paramref name="name"/> is a valid for a notebook.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see langword="true"/> if the specified <paramref name="name"/> is not null, empty, white space or contains any characters from <see cref="InvalidNotebookChars"/>; otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="InvalidNotebookChars"/>
        public static bool IsNotebookNameValid(string name) 
            => !string.IsNullOrWhiteSpace(name) && !InvalidNotebookChars.Any(name.Contains);

        /// <summary>
        /// Returns a value that indicates whether the supplied <paramref name="name"/> is a valid for a section.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see langword="true"/> if the specified <paramref name="name"/> is not null, empty, white space or contains any characters from <see cref="InvalidSectionChars"/>; otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="InvalidSectionChars"/>
        public static bool IsSectionNameValid(string name) 
            => !string.IsNullOrWhiteSpace(name) && !InvalidSectionChars.Any(name.Contains);

        /// <summary>
        /// Returns a value that indicates whether the supplied <paramref name="name"/> is a valid for a section group.
        /// </summary>
        /// <returns><see langword="true"/> if the specified <paramref name="name"/> is not null, empty, white space or contains any characters from <see cref="InvalidSectionGroupChars"/>; otherwise, <see langword="false"/>.</returns>
        /// <param name="name"></param>
        /// <seealso cref="InvalidSectionGroupChars"/>
        public static bool IsSectionGroupNameValid(string name) 
            => !string.IsNullOrWhiteSpace(name) && !InvalidSectionGroupChars.Any(name.Contains);

        #endregion

        #region Special Folder Locations
        /// <summary>
        /// Retrieves the path on disk to the default notebook folder location, this is where new notebooks are created and saved to.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <returns>The path to the default notebook folder location.</returns>
        public static string GetDefaultNotebookLocation(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slDefaultNotebookFolder, out string path);
            return path;
        }
        /// <summary>
        /// Retrieves the path on disk to the back up folder location.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <returns>The path on disk to the back up folder location.</returns>
        public static string GetBackUpLocation(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slBackUpFolder, out string path);
            return path;
        }
        /// <summary>
        /// Retrieves the folder path on disk to the unfiled notes section, this is also where quick notes are created and saved to.
        /// </summary>
        /// <param name="oneNote"><inheritdoc cref="GetNotebooks(IApplication)" path="/param[@name='oneNote']"/></param>
        /// <returns>The folder path on disk to the unfiled notes section.</returns>
        public static string GetUnfiledNotesSection(IApplication oneNote)
        {
            oneNote.GetSpecialLocation(SpecialLocation.slUnfiledNotesSection, out string path);
            return path;
        }

        #endregion
    }
}
