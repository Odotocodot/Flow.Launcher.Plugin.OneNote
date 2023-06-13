using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public class OneNoteApplication : IDisposable
    {
        private Application oneNote;
        private bool disposedValue;
        public OneNoteApplication() : this(true) {}
        public OneNoteApplication(bool init)
        {
           if(init)
               Init();
        }

        private bool hasInstance;
        public bool HasInstance => hasInstance;

        public void Init()
        {
            int attempt = 0;

            while (!hasInstance)
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
            hasInstance = oneNote != null && Marshal.IsComObject(oneNote);
        }
        public IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            COMInstanceCheck();
            return OneNoteParser.GetNotebooks(oneNote);
        }
        public void OpenInOneNote(IOneNoteItem item)
        {
            COMInstanceCheck();
            OneNoteParser.OpenInOneNote(oneNote, item.ID);
        }
        public void SyncItem(IOneNoteItem item)
        {
            COMInstanceCheck();
            OneNoteParser.SyncItem(oneNote, item.ID);
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

        public void CreateQuickNote()
        {
            COMInstanceCheck();
            OneNoteParser.CreateQuickNote(oneNote, true);
        }
        public void CreatePage(OneNoteSection section, string pageTitle)
        {
            COMInstanceCheck();
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
        }
        public void CreateSection(IOneNoteItem parent, string sectionName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        public void CreateSectionGroup(IOneNoteItem parent, string sectionGroupName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        public void CreateNotebook(string notebookName)
        {
            COMInstanceCheck();
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
        }

        private void COMInstanceCheck()
        {
            if (!hasInstance)
                throw new InvalidOperationException("The COM Object instance has not been set. Make sure OneNoteProvider.Init() has been called before.");
        }
        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    hasInstance = false;
                }

                if(oneNote != null)
                {
                    Marshal.ReleaseComObject(oneNote);
                    oneNote = null;
                }
                disposedValue = true;
            }
        }
        ~OneNoteApplication()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
