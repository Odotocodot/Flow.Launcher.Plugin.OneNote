using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public class OneNoteProvider : IDisposable
    {
        private Application oneNote;
        private bool disposedValue;
        //public OneNoteProvider()
        //{
        //    Init();
        //}

        //public OneNoteProvider(bool init)
        //{
        //    if(init)
        //    {
        //        Init();
        //    }
        //}

        //TODO: convert to field, add to each method?
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
        public IEnumerable<OneNoteNotebook> GetNotebooks()
        {
            COMInstanceCheck();
            return OneNoteParser.GetNotebooks(oneNote);
        }

        private void COMInstanceCheck()
        {
            if (!HasInstance)
                throw new InvalidOperationException("The COM Object instance has not been set. Make sure OneNoteProvider.Init() has been called before.");
        }

        public void OpenInOneNote(IOneNoteItem item)
        {
            OneNoteParser.OpenInOneNote(oneNote, item.ID);
        }
        public void SyncItem(IOneNoteItem item)
        {
            OneNoteParser.SyncItem(oneNote, item.ID);
        }

        public IEnumerable<IOneNoteItem> Traverse()
        {
            return GetNotebooks().Traverse();
        }
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            return GetNotebooks().Traverse(predicate);
        }

        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            return OneNoteParser.FindPages(oneNote, searchString);
        }
        public IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            return OneNoteParser.FindPages(oneNote, searchString, scope);
        }
        public string GetDefaultNotebookLocation()
        {
            return OneNoteParser.GetDefaultNotebookLocation(oneNote);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //if (disposing)
                //{
                //}

                if(oneNote != null)
                {
                    Marshal.ReleaseComObject(oneNote);
                    oneNote = null;
                }
                disposedValue = true;
            }
        }
        ~OneNoteProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
