using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote
{
    public class OneNoteApplication
    {
        private Application oneNote;

        public OneNoteApplication(bool releaseOnOpen)
        {
            ReleaseOnOpen = releaseOnOpen;
            Init();
        }

        public bool HasInstance { get; private set; }
        /// <summary>
        /// Releases the <see cref="Application"/> instance when OneNote is opened by the class.
        /// </summary>
        public bool ReleaseOnOpen { get; private set; }

        public IEnumerable<OneNoteNotebookExt> Notebooks => OneNoteProvider.GetNotebooks(oneNote);
        public string DefaultNotebookLocation => OneNoteProvider.GetDefaultNotebookLocation(oneNote);
        public IEnumerable<OneNotePage> Pages => OneNoteProvider.GetPages(oneNote);

        public void Init()
        {
            if(!HasInstance)
            {
                oneNote = Util.TryCatchAndRetry<Application, COMException>(
                            () => new Application(),
                            TimeSpan.FromMilliseconds(100),
                            3,
                            ex => Trace.TraceError(ex.Message));
            }
            HasInstance = true;
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
        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            return OneNoteProvider.FindPages(oneNote, searchString);
        }

        public void CreatePage(OneNoteSection section, string pageTitle)
        {
            OneNoteProvider.CreatePage(oneNote, section, pageTitle, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateQuickNote()
        {
            OneNoteProvider.CreateQuickNote(oneNote, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateSection(OneNoteNotebook notebook, string sectionName)
        {
            OneNoteProvider.CreateSection(oneNote, notebook, sectionName, true);
            if (ReleaseOnOpen)
                Release();
        }

        public void CreateNotebook(string notebookName)
        {
            OneNoteProvider.CreateNotebook(oneNote, notebookName, true);
            if (ReleaseOnOpen)
                Release();
        }
    }
}
