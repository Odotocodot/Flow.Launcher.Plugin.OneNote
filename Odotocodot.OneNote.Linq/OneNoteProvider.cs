using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    public class OneNoteProvider
    {
        private Application oneNote;

        public OneNoteProvider(bool releaseOnOpen)
        {
            ReleaseOnOpen = releaseOnOpen;
            HasInstance = false;
        }

        public bool HasInstance { get; private set; }
        /// <summary>
        /// Releases the <see cref="Application"/> instance when OneNote is opened by the class.
        /// </summary>
        public bool ReleaseOnOpen { get; private set; }

        public IEnumerable<OneNoteNotebook> Notebooks => OneNoteParser.GetNotebooks(oneNote);
        public string DefaultNotebookLocation => OneNoteParser.GetDefaultNotebookLocation(oneNote);
        public IEnumerable<OneNotePage> Pages => OneNoteParser.GetPages(oneNote);

        public void Init()
        {
            if(!HasInstance)
            {
                try
                {
                    oneNote = Util.TryCatchAndRetry<Application, COMException>(
                                () => new Application(),
                                TimeSpan.FromMilliseconds(100),
                                3,
                                ex => Trace.TraceError(ex.Message));
                }
                finally
                {
                    HasInstance = oneNote != null;        
                }
            }
        }
        public void Release() 
        {
            if (oneNote != null)
            {
                Marshal.ReleaseComObject(oneNote);
            }
            HasInstance = false;
        }

        public void OpenInOneNote(IOneNoteItem item)
        {
            oneNote.NavigateTo(item.ID);
            if(ReleaseOnOpen)
                Release();
        }

        public IEnumerable<IOneNoteItem> Traverse()
        {
            foreach (IOneNoteItem notebook in Notebooks)
            {
                foreach (var child in notebook.Traverse())
                {
                    yield return child;
                }
            }
        }
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            foreach (IOneNoteItem notebook in Notebooks)
            {
                foreach (var child in notebook.Traverse(predicate))
                {
                    yield return child;
                }
            }
        }

        public void SyncItem(IOneNoteItem item)
        {
            oneNote.SyncHierarchy(item.ID);
        }
        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            return OneNoteParser.FindPages(oneNote, null, searchString);
        }
        public IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            return OneNoteParser.FindPages(oneNote, scope, searchString);
        }

        public void CreatePage(OneNoteSection section, string pageTitle)
        {
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateQuickNote()
        {
            OneNoteParser.CreateQuickNote(oneNote, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateSection(IOneNoteItem parent, string sectionName)
        {
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateSectionGroup(IOneNoteItem parent, string sectionGroupName)
        {
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateNotebook(string notebookName)
        {
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
            if (ReleaseOnOpen)
                Release();
        }
    }
}
