using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public class OneNoteApplication
    {
        private static Application oneNote;
        private static bool hasCOMInstance;
        public static bool HasCOMInstance => hasCOMInstance;
        public static void Init()
        {
            int attempt = 0;

            while (!hasCOMInstance)
            {
                try
                {
                    oneNote = new Application();
                    hasCOMInstance = oneNote != null && Marshal.IsComObject(oneNote);
                }
                catch (COMException ex) when (attempt++ < 3)
                {
                    Trace.TraceError(ex.Message);
                    Thread.Sleep(100);
                }
            }
        }
        public IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            COMInstanceCheck();
            return OneNoteParser.GetNotebooks(oneNote);
        }
        public static void OpenInOneNote(IOneNoteItem item)
        {
            COMInstanceCheck();
            OneNoteParser.OpenInOneNote(oneNote, item);
        }
        public static void SyncItem(IOneNoteItem item)
        {
            COMInstanceCheck();
            OneNoteParser.SyncItem(oneNote, item);
        }

        public IEnumerable<IOneNoteItem> Traverse()
        {
            COMInstanceCheck();
            return GetNotebooks().Traverse();
        }
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            COMInstanceCheck();
            return GetNotebooks().Traverse(predicate);
        }

        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            COMInstanceCheck();
            return OneNoteParser.FindPages(oneNote, searchString);
        }
        public IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            COMInstanceCheck();
            return OneNoteParser.FindPages(oneNote, searchString, scope);
        }
        public string GetDefaultNotebookLocation()
        {
            COMInstanceCheck();
            return OneNoteParser.GetDefaultNotebookLocation(oneNote);
        }

        public static void CreateQuickNote()
        {
            COMInstanceCheck();
            OneNoteParser.CreateQuickNote(oneNote, true);
        }
        public static void CreatePage(OneNoteSection section, string pageTitle)
        {
            COMInstanceCheck();
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
        }
        public static void CreateSection(OneNoteSectionGroup parent, string sectionName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        public static void CreateSection(OneNoteNotebook parent, string sectionName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        public static void CreateSectionGroup(OneNoteSectionGroup parent, string sectionGroupName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        public static void CreateSectionGroup(OneNoteNotebook parent, string sectionGroupName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        public static void CreateNotebook(string notebookName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
        }

        private static void COMInstanceCheck()
        {
            if (!hasCOMInstance)
                throw new InvalidOperationException($"The COM Object instance has not been set. Make sure {nameof(OneNoteApplication)}.{nameof(Init)} has been called beforehand.");
        }
        
        public static void ReleaseCOMInstance()
        {
            if (oneNote != null)
            {
                Marshal.ReleaseComObject(oneNote);
                oneNote = null;
            }
        }
    }
}
