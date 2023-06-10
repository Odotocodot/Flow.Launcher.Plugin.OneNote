using Microsoft.Office.Interop.OneNote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Odotocodot.OneNote.Linq
{
    public static class OneNoteParser
    {
        private const string NamespacePrefix = "one";

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
        public static IEnumerable<OneNoteNotebook> GetNotebooks(Application oneNote)
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
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, string searchString)
        {
            oneNote.FindPages(null, searchString, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName(OneNoteItemType.Notebook))
                              .Select(element => new OneNoteNotebook(element))
                              .GetPages();
        }

        /// <inheritdoc cref="FindPages"/>
        /// <remarks> 
        /// Passing in <paramref name="scope"/> allows for searching within that specific OneNote item. <br/>
        /// If <paramref name="scope" /> is <see langword="null" /> this method is equivalent to <see cref="FindPages(Application,string)"/>
        /// </remarks>
        /// <param name="oneNote"></param>
        /// <param name="searchString"></param>
        /// <param name="scope"></param>
        public static IEnumerable<OneNotePage> FindPages(Application oneNote, string searchString, IOneNoteItem scope)
        {
            ArgumentNullException.ThrowIfNull(scope, nameof(scope));

            oneNote.FindPages(scope.ID, searchString, out string xml);

            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName(scope.ItemType))
                              .Select<XElement,IOneNoteItem>(element =>
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

        public static void OpenInOneNote(Application oneNote, string id)
        {
            oneNote.NavigateTo(id);
        }

        public static void SyncItem(Application oneNote, string id)
        {
            oneNote.SyncHierarchy(id);
        }

        public static void 

        #region Creating OneNote Items
        public static void CreatePage(Application oneNote, OneNoteSection section, string pageTitle, bool openImmediately)
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
