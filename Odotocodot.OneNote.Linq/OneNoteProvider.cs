﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public class OneNoteProvider
    {
        private Application oneNote;
        public bool HasInstance => oneNote != null && Marshal.IsComObject(oneNote);

        public void Init()
        {
            int attempt = 0;

            while (!HasInstance)
            {
                try
                {
                    oneNote = new Application();
                }
                catch (COMException ex) when (attempt++ < 3)
                {
                    Trace.TraceError(ex.Message);
                    Thread.Sleep(100);
                }
            }           
        }
        public void Release()
        {
            if (oneNote != null)
            {
                Marshal.ReleaseComObject(oneNote);
                oneNote = null;
            }
        }

        /// <summary>
        /// Automatically releases the OneNote COM Object after use
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CallOneNoteSafely<T>(Func<OneNoteProvider, T> action, Func<COMException, T> onException = null)
        {
            OneNoteProvider oneNote = null;
            try
            {
                oneNote = new OneNoteProvider();
                oneNote.Init();
                return action(oneNote);
            }
            catch (COMException ex) when (onException != null)
            {
                return onException(ex);
            }
            finally
            {
                oneNote.Release();
            }
        }

        public IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            return OneNoteParser.GetNotebooks(oneNote);
        }

        public string GetDefaultNotebookLocation()
        {
            return OneNoteParser.GetDefaultNotebookLocation(oneNote);
        }

        public void OpenInOneNote(IOneNoteItem item)
        {
            OneNoteParser.OpenInOneNote(oneNote, item.ID);
        }

        public IEnumerable<IOneNoteItem> Traverse()
        {
            return GetNotebooks().Traverse();
        }
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            return GetNotebooks().Traverse(predicate);
        }

        public void SyncItem(IOneNoteItem item)
        {
            OneNoteParser.SyncItem(oneNote, item.ID);
        }

        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            return OneNoteParser.FindPages(oneNote, searchString);
        }
        public IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            return OneNoteParser.FindPages(oneNote, searchString, scope);
        }


        public void CreateQuickNote()
        {
            OneNoteParser.CreateQuickNote(oneNote, true);
        }
        public void CreatePage(OneNoteSection section, string pageTitle)
        {
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
        }

        public void CreateSection(IOneNoteItem parent, string sectionName)
        {
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }

        public void CreateSectionGroup(IOneNoteItem parent, string sectionGroupName)
        {
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }

        public void CreateNotebook(string notebookName)
        {
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
        }
    }
}
