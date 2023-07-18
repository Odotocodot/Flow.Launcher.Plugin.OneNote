﻿using Microsoft.Office.Interop.OneNote;
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
        public static readonly char[] InvalidSectionChars = "\\/*?\"|<>:%#&".ToCharArray();
        public static readonly char[] InvalidSectionGroupChars = InvalidSectionChars;

        private static readonly Lazy<Dictionary<Type, XName>> xNames = new Lazy<Dictionary<Type, XName>>(() =>
        {
            var namespaceUri = "http://schemas.microsoft.com/office/onenote/2013/onenote";
            return new Dictionary<Type, XName>
            {
                {typeof(OneNoteNotebook),       XName.Get("Notebook",       namespaceUri)},
                {typeof(OneNoteSectionGroup),   XName.Get("SectionGroup",   namespaceUri)},
                {typeof(OneNoteSection),        XName.Get("Section",        namespaceUri)},
                {typeof(OneNotePage),           XName.Get("Page",           namespaceUri)}
            };
        });

        internal static XName GetXName<T>() where T : IOneNoteItem => xNames.Value[typeof(T)];


        /// <summary>
        /// Hierarchy of notebooks with section groups, sections and Pages.
        /// </summary>
        public static IEnumerable<OneNoteNotebook> GetNotebooks(IApplication oneNote)
        {
            oneNote.GetHierarchy(null, HierarchyScope.hsPages, out string xml);
            var rootElement = XElement.Parse(xml);
            return rootElement.Elements(GetXName<OneNoteNotebook>())
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
            return rootElement.Elements(GetXName<OneNoteNotebook>())
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

            IOneNoteItem root = scope switch
            {
                OneNoteNotebook => new OneNoteNotebook(rootElement),
                OneNoteSectionGroup => new OneNoteSectionGroup(rootElement, scope.Parent),
                OneNoteSection => new OneNoteSection(rootElement, scope.Parent),
                OneNotePage => new OneNotePage(rootElement, (OneNoteSection)scope.Parent),
                _ => null,
            };
            return root.GetPages();
        }

        private static void ValidateSearchString(string searchString)
        {
            ArgumentNullException.ThrowIfNull(searchString, nameof(searchString));

            if (string.IsNullOrWhiteSpace(searchString))
                throw new ArgumentException("Search string cannot be empty or only whitespace", nameof(searchString));

            if (!char.IsLetterOrDigit(searchString[0]))
                throw new ArgumentException("The first character of the search must be a letter or a digit", nameof(searchString));
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
                        throw new ArgumentException($"Invalid notebook name. Notebook names cannot contain the symbols: \n {string.Join(' ', InvalidNotebookChars)}");

                    path = Path.Combine(GetDefaultNotebookLocation(oneNote), title);
                    createFileType = CreateFileType.cftNotebook;
                    break;
                case nameof(OneNoteSectionGroup):
                    if (!IsSectionGroupTitleValid(title))
                        throw new ArgumentException($"Invalid section group name. Section groups names cannot contain the symbols: \n {string.Join(' ', InvalidSectionGroupChars)}");

                    path = title;
                    createFileType = CreateFileType.cftFolder;
                    break;
                case nameof(OneNoteSection):
                    if (!IsSectionTitleValid(title))
                        throw new ArgumentException($"Invalid section name. Section names cannot contain the symbols: \n {string.Join(' ', InvalidSectionChars)}");

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
