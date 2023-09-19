using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// A static wrapper class around the <see cref="Application"/> class, allowing for easy acquirement and
    /// release of a OneNote COM object, whilst avoiding duplicate instances. In addition to exposing the 
    /// <see href="https://learn.microsoft.com/en-us/office/client-developer/onenote/application-interface-onenote">OneNote's API</see>
    /// </summary>
    /// <remarks>A <see cref="Application">OneNote COM object</see> is required to access any of the OneNote API.</remarks>
    public static class OneNoteApplication
    {
        private static Application oneNote;
        private static bool hasComInstance = false;
        /// <summary>
        /// Indicates whether the class has a usable <see cref="Application">COM instance</see>.
        /// </summary>
        /// <remarks>When <see langword="true"/> a OneNote process should be visible in the Task Manager.</remarks>
        /// <seealso cref="Init"/>
        /// <seealso cref="ReleaseComInstance"/>
        public static bool HasComInstance => hasComInstance;

        /// <summary>
        /// Initialises the static class by acquiring a <see cref="Application">OneNote COM object</see>.
        /// </summary>
        /// <exception cref="COMException">Thrown if an error occurred when trying to get the 
        /// <see cref="Application">OneNote COM object</see> or the number of attempts in doing 
        /// so exceeded the limit.</exception>
        /// <seealso cref="HasComInstance"/>
        /// <seealso cref="ReleaseComInstance"/>
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

        /// <inheritdoc cref="OneNoteParser.FindPages(IApplication, string)"/>
        public static IEnumerable<OneNotePage> FindPages(string search)
        {
            Init();
            return OneNoteParser.FindPages(oneNote, search);
        }
        /// <inheritdoc cref="OneNoteParser.FindPages(IApplication, string, IOneNoteItem)"/>
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
        /// <inheritdoc cref="OneNoteParser.CreateSection(IApplication, OneNoteSectionGroup, string, bool)"/>
        public static void CreateSection(OneNoteSectionGroup parent, string sectionName)
        {
            Init();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        /// <inheritdoc cref="OneNoteParser.CreateSection(IApplication, OneNoteNotebook, string, bool)"/>
        public static void CreateSection(OneNoteNotebook parent, string sectionName)
        {
            Init();
            OneNoteParser.CreateSection(oneNote, parent, sectionName, true);
        }
        /// <inheritdoc cref="OneNoteParser.CreateSectionGroup(IApplication, OneNoteSectionGroup, string, bool)"/>
        public static void CreateSectionGroup(OneNoteSectionGroup parent, string sectionGroupName)
        {
            Init();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }
        /// <inheritdoc cref="OneNoteParser.CreateSectionGroup(IApplication, OneNoteNotebook, string, bool)"/>
        public static void CreateSectionGroup(OneNoteNotebook parent, string sectionGroupName)
        {
            Init();
            OneNoteParser.CreateSectionGroup(oneNote, parent, sectionGroupName, true);
        }

        /// <inheritdoc cref="OneNoteParser.CreateNotebook(IApplication, string, bool)"/>
        public static void CreateNotebook(string notebookName)
        {
            Init();
            OneNoteParser.CreateNotebook(oneNote, notebookName, true);
        }

        /// <summary>
        /// Releases the <see cref="Application">OneNote COM object</see> freeing memory.
        /// </summary>
        /// <seealso cref="Init"/>
        /// <seealso cref="HasComInstance"/>
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
