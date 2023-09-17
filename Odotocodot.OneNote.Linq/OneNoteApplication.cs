using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    //wrapper around com object for easy release of com object
    public static class OneNoteApplication
    {
        private static Application oneNote;
        private static bool hasComInstance = false;
        public static bool HasComInstance => hasComInstance;
        public static void Init()
        {
            int attempt = 0;

            while (!hasComInstance)
            {
                try
                {
                    oneNote = new Application();
                    hasComInstance = oneNote != null && Marshal.IsComObject(oneNote);
                }
                catch (COMException ex) when (attempt++ < 3)
                {
                    Trace.TraceError(ex.Message);
                    Thread.Sleep(100);
                }
                catch (COMException ex) when (attempt == 3)
                {
                    throw new COMException("Unable to acquire a OneNote COM object", ex);
                }
            }
        }
        /// <inheritdoc cref="OneNoteParser.GetNotebooks(IApplication)"/>
        public static IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            Init();
            return OneNoteParser.GetNotebooks(oneNote);
        }
        /// <inheritdoc cref="OneNoteParser.OpenInOneNote(IApplication, IOneNoteItem)"/>
        public static void OpenInOneNote(IOneNoteItem item)
        {
            Init();
            OneNoteParser.OpenInOneNote(oneNote, item);
        }
        /// <inheritdoc cref="OneNoteParser.SyncItem(IApplication, IOneNoteItem)"/>
        public static void SyncItem(IOneNoteItem item)
        {
            Init();
            OneNoteParser.SyncItem(oneNote, item);
        }
        /// <inheritdoc cref="OneNoteParser.GetPageContent(IApplication, OneNotePage)"/>
        public static string GetPageContent(OneNotePage page)
        {
            Init();
            return OneNoteParser.GetPageContent(oneNote, page);
        }

        public static IEnumerable<IOneNoteItem> Traverse()
        {
            Init();
            return GetNotebooks().Traverse();
        }
        public static IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            Init();
            return GetNotebooks().Traverse(predicate);
        }

        /// <inheritdoc cref="OneNoteParser.FindPages(IApplication, string)"/>
        public static IEnumerable<OneNotePage> FindPages(string search)
        {
            Init();
            return OneNoteParser.FindPages(oneNote, search);
        }
        public static IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string search)
        {
            Init();
            return OneNoteParser.FindPages(oneNote, search, scope);
        }

        /// <inheritdoc cref="OneNoteParser.GetDefaultNotebookLocation(IApplication)"/>
        public static string GetDefaultNotebookLocation()
        {
            Init();
            return OneNoteParser.GetDefaultNotebookLocation(oneNote);
        }

        /// <inheritdoc cref="OneNoteParser.CreateQuickNote(IApplication, bool)"/>
        public static void CreateQuickNote()
        {
            Init();
            OneNoteParser.CreateQuickNote(oneNote, true);
        }

        /// <inheritdoc cref="OneNoteParser.CreatePage(IApplication, OneNoteSection, string, bool)"/>
        public static void CreatePage(OneNoteSection section, string pageTitle)
        {
            Init();
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
        }
        public static void CreateSection(OneNoteSectionGroup parent, string sectionName)
        {
            Init();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        public static void CreateSection(OneNoteNotebook parent, string sectionName)
        {
            Init();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        public static void CreateSectionGroup(OneNoteSectionGroup parent, string sectionGroupName)
        {
            Init();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        public static void CreateSectionGroup(OneNoteNotebook parent, string sectionGroupName)
        {
            Init();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        public static void CreateNotebook(string notebookName)
        {
            Init();
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
        }

        public static void ReleaseComInstance()
        {
            if (hasComInstance)
            {
                Marshal.ReleaseComObject(oneNote);
                oneNote = null;
                hasComInstance = false;
            }
        }
    }
}
