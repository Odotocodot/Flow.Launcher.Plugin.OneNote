﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public static class OneNoteApplication
    {
        private static Application oneNote;
        private static bool hasCOMInstance = false;
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
                catch (COMException ex) when (attempt == 3)
                {
                    throw new COMException("Unable to acquire a OneNote COM object", ex);
                }
            }
        }
        public static IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            Init();
            return OneNoteParser.GetNotebooks(oneNote);
        }
        public static void OpenInOneNote(IOneNoteItem item)
        {
            Init();
            OneNoteParser.OpenInOneNote(oneNote, item);
        }
        public static void SyncItem(IOneNoteItem item)
        {
            Init();
            OneNoteParser.SyncItem(oneNote, item);
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

        public static IEnumerable<OneNotePage> FindPages(string searchString)
        {
            Init();
            return OneNoteParser.FindPages(oneNote, searchString);
        }
        public static IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            Init();
            return OneNoteParser.FindPages(oneNote, searchString, scope);
        }
        public static string GetDefaultNotebookLocation()
        {
            Init();
            return OneNoteParser.GetDefaultNotebookLocation(oneNote);
        }

        public static void CreateQuickNote()
        {
            Init();
            OneNoteParser.CreateQuickNote(oneNote, true);
        }
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
        
        public static void ReleaseCOMInstance()
        {
            if (hasCOMInstance)
            {
                Marshal.ReleaseComObject(oneNote);
                oneNote = null;
                hasCOMInstance = false;
            }
        }
    }
}
