using Microsoft.Office.Interop.OneNote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public static class OneNoteParser
    {
        private const string NamespacePrefix = "one";

        public static readonly char[] InvalidNotebookChars = "\\/*?\"|<>:%#.".ToCharArray();
        public static readonly char[] InvalidSectionChars  = "\\/*?\"|<>:%#&".ToCharArray();
        public static readonly char[] InvalidSectionGroupChars = InvalidSectionChars;

        private static readonly Lazy<XName[]> xNames = new Lazy<XName[]>(() =>
        {
            var itemTypes = Enum.GetValues<OneNoteItemType>();
            var xNames = new XName[itemTypes.Length];
            for (int i = 0; i < itemTypes.Length; i++)
            {
                xNames[(int)itemTypes[i]] = XName.Get(itemTypes[i].ToString(), "http://schemas.microsoft.com/office/onenote/2013/onenote");
            }
            return xNames;
        });
        internal static XName GetXName(OneNoteItemType itemType)
        {
            return xNames.Value[(int)itemType];
        }


        /// <summary>
        /// Hierarchy of notebooks with section groups, sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(IApplication oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName(OneNoteItemType.Notebook))
                              .Select(element => new OneNoteNotebook(element));
        }

        /// <summary>
        /// Returns a collection of pages that match the specified query term. <br/>
        /// <paramref name="searchString" /> should be exactly the same string that you would type into the search box in the OneNote UI. You can use bitwise operators, such as AND and OR, which must be all uppercase.
        /// </summary>
        /// <param name="oneNote"></param>
        /// <param name="searchString"></param>
        public static IEnumerable<OneNotePage> FindPages(IApplication oneNote, string searchString)
        {
            ValidateSearchString(searchString);

            oneNote.FindPages(null, searchString, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName(OneNoteItemType.Notebook))
                              .Select(element => new OneNoteNotebook(element))
                              .GetPages();
        }

        /// <inheritdoc cref="FindPages"/>
        /// <remarks> 
        /// Passing in <paramref name="scope"/> allows for searching within that specific OneNote item. <br/>
        /// If <paramref name="scope" /> is <see langword="null" /> this method is equivalent to <see cref="FindPages(IApplication,string)"/>
        /// </remarks>
        /// <param name="oneNote"></param>
        /// <param name="searchString"></param>
        /// <param name="scope"></param>
        public static IEnumerable<OneNotePage> FindPages(IApplication oneNote, string searchString, IOneNoteItem scope)
        {
            ArgumentNullException.ThrowIfNull(scope, nameof(scope));

            ValidateSearchString(searchString);

            oneNote.FindPages(scope.ID, searchString, out string xml);

            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName(scope.ItemType))
                              .Select<XElement, IOneNoteItem>(element =>
                              {
                                  return scope.ItemType switch
                                  {
                                      OneNoteItemType.Notebook => new OneNoteNotebook(element),
                                      OneNoteItemType.SectionGroup => new OneNoteSectionGroup(element, scope.Parent),
                                      OneNoteItemType.Section => new OneNoteSection(element, scope.Parent),
                                      OneNoteItemType.Page => new OneNotePage(element, (OneNoteSection)scope.Parent),
                                      _ => null,
                                  };
                              })
                              .GetPages();
        }

        private static void ValidateSearchString(string searchString)
        {
            ArgumentNullException.ThrowIfNull(searchString, nameof(searchString));

            if (string.IsNullOrWhiteSpace(searchString))
                throw new ArgumentException("Search string cannot be empty or only whitespace", nameof(searchString));

            if (!char.IsLetterOrDigit(searchString[0]))
                throw new ArgumentException("The first character of the search must be a letter or digit", nameof(searchString));
        }

        public static void OpenInOneNote(IApplication oneNote, IOneNoteItem item)
        {
            oneNote.NavigateTo(item.ID);
        }
        public static void SyncItem(IApplication oneNote, IOneNoteItem item)
        {
            oneNote.SyncHierarchy(item.ID);
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

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }
        public static void CreateQuickNote(IApplication oneNote, bool openImmediately)
        {
            var path = GetUnfiledNotesSection(oneNote);
            oneNote.OpenHierarchy(path, null, out string sectionID, CreateFileType.cftNone);
            oneNote.CreateNewPage(sectionID, out string pageID, NewPageStyle.npsDefault);

            if(openImmediately)
                oneNote.NavigateTo(pageID);
        }

        private static void CreateItemBase(IApplication oneNote, IOneNoteItem parent, string title, bool openImmediately, OneNoteItemType newItemType)
        {
            ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));

            string path = string.Empty;
            CreateFileType createFileType = CreateFileType.cftNone;
            switch (newItemType)
            {
                case OneNoteItemType.Notebook:
                    if (!IsNotebookTitleValid(title))
                        throw new ArgumentException($"Invalid notebook name. Notebook names cannot contain the symbols: \n {string.Join(' ', InvalidNotebookChars)}");

                    path = Path.Combine(GetDefaultNotebookLocation(oneNote), title);
                    createFileType = CreateFileType.cftNotebook;
                    break;
                case OneNoteItemType.SectionGroup:
                    if (!IsSectionGroupTitleValid(title))
                        throw new ArgumentException($"Invalid section group name. Section groups names cannot contain the symbols: \n {string.Join(' ', InvalidSectionGroupChars)}");
                    
                    path = title;
                    createFileType = CreateFileType.cftFolder;
                    break;
                case OneNoteItemType.Section:
                    if (!IsSectionTitleValid(title))
                        throw new ArgumentException($"Invalid section name. Section names cannot contain the symbols: \n {string.Join(' ', InvalidSectionChars)}");

                    path = title + ".one";
                    createFileType = CreateFileType.cftSection;
                    break;
            }

            oneNote.OpenHierarchy(path, parent?.ID, out string newItemID, createFileType);

            if(openImmediately)
                oneNote.NavigateTo(newItemID);

        }

        public static void CreateSection(IApplication oneNote, OneNoteSectionGroup parent, string sectionName, bool openImmediately)
        {
            CreateItemBase(oneNote, parent, sectionName, openImmediately, OneNoteItemType.Section);
        }
        public static void CreateSection(IApplication oneNote, OneNoteNotebook parent, string sectionName, bool openImmediately)
        {
            CreateItemBase(oneNote, parent, sectionName, openImmediately, OneNoteItemType.Section);
        }
        public static void CreateSectionGroup(IApplication oneNote, OneNoteSectionGroup parent, string sectionGroupName, bool openImmediately)
        {
            CreateItemBase(oneNote, parent, sectionGroupName, openImmediately, OneNoteItemType.SectionGroup);
        }
        public static void CreateSectionGroup(IApplication oneNote, OneNoteNotebook parent, string sectionGroupName, bool openImmediately)
        {
            CreateItemBase(oneNote, parent, sectionGroupName, openImmediately, OneNoteItemType.SectionGroup);
        }
        public static void CreateNotebook(IApplication oneNote, string notebookName, bool openImmediately)
        {
            CreateItemBase(oneNote, null, notebookName, openImmediately, OneNoteItemType.Notebook);
        }

        public static bool IsNotebookTitleValid(string notebookTitle)
        {
            return notebookTitle.IndexOfAny(InvalidNotebookChars) == -1;
        }
        public static bool IsSectionTitleValid(string sectionTitle)
        {
            return sectionTitle.IndexOfAny(InvalidSectionChars) == -1;
        }
        public static bool IsSectionGroupTitleValid(string sectionGroupTitle)
        {
            return sectionGroupTitle.IndexOfAny(InvalidSectionGroupChars) == -1;
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
