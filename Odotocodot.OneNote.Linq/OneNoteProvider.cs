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

        public OneNoteProvider()
        {
            HasInstance = false;
        }

        public bool HasInstance { get; private set; }

        public IEnumerable<OneNoteNotebook> Notebooks => OneNoteParser.GetNotebooks(oneNote);
        public string DefaultNotebookLocation => OneNoteParser.GetDefaultNotebookLocation(oneNote);
        public IEnumerable<OneNotePage> Pages => OneNoteParser.GetPages(oneNote);

        public void Init()
        {
            if (!HasInstance)
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
                Marshal.FinalReleaseComObject(oneNote);
            }
            HasInstance = false;
        }

        public void OpenInOneNote(IOneNoteItem item)
        {
            oneNote.NavigateTo(item.ID);
        }

        public IEnumerable<IOneNoteItem> Traverse()
        {
            return Notebooks.Traverse();
        }
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            return Notebooks.Traverse(predicate);
        }

        public void SyncItem(IOneNoteItem item)
        {
            oneNote.SyncHierarchy(item.ID);
        }
        public IEnumerable<OneNotePage> FindPages(string searchString)
        {
            return OneNoteParser.FindPages(oneNote, searchString);
        }
        public IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string searchString)
        {
            return OneNoteParser.FindPages(oneNote, scope, searchString);
        }

        public void CreatePage(OneNoteSection section, string pageTitle)
        {
            OneNoteParser.CreatePage(oneNote, section, pageTitle, true);
        }

        public void CreateQuickNote()
        {
            OneNoteParser.CreateQuickNote(oneNote, true);
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
