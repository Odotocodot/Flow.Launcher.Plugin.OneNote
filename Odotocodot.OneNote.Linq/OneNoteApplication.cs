using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.OneNote;

namespace Odotocodot.OneNote.Linq
{
    /// <summary>
    /// A static wrapper class around the <see cref="Application"/> class, allowing for <see cref="Lazy{T}">lazy</see> acquirement and
    /// release of a OneNote COM object. In addition to exposing the 
    /// <see href="https://learn.microsoft.com/en-us/office/client-developer/onenote/application-interface-onenote">OneNote's API</see>
    /// </summary>
    /// <remarks>A <see cref="Application">OneNote COM object</see> is required to access any of the OneNote API.</remarks>
    public static class OneNoteApplication
    {
        private static Lazy<Application> lazyOneNote = new (() => new Application(), LazyThreadSafetyMode.ExecutionAndPublication);
        private static Application OneNote => lazyOneNote.Value;
        /// <summary>
        /// Indicates whether the class has a usable <see cref="Application">COM instance</see>.
        /// </summary>
        /// <remarks>When <see langword="true"/> a "Microsoft OneNote" process should be visible in the Task Manager.</remarks>
        /// <seealso cref="Init"/>
        /// <seealso cref="ReleaseComObject"/>
        public static bool HasComObject => lazyOneNote.IsValueCreated;

        /// <summary>
        /// Forceable initialises the static class by acquiring a <see cref="Application">OneNote COM object</see>.
        /// </summary>
        /// <exception cref="COMException">Thrown if an error occurred when trying to get the 
        /// <see cref="Application">OneNote COM object</see> or the number of attempts in doing 
        /// so exceeded the limit.</exception>
        /// <seealso cref="HasComObject"/>
        /// <seealso cref="ReleaseComObject"/>
        public static void Init()
        {
            if (!lazyOneNote.IsValueCreated)
            {
                _ = OneNote;
            }
        }
        /// <inheritdoc cref="OneNoteParser.GetNotebooks(IApplication)"/>
        public static IEnumerable<OneNoteNotebook> GetNotebooks() => OneNoteParser.GetNotebooks(OneNote);

        /// <inheritdoc cref="OneNoteParser.OpenInOneNote(IApplication, IOneNoteItem)"/>
        public static void OpenInOneNote(IOneNoteItem item) => OneNoteParser.OpenInOneNote(OneNote, item);

        /// <inheritdoc cref="OneNoteParser.SyncItem(IApplication, IOneNoteItem)"/>
        public static void SyncItem(IOneNoteItem item) => OneNoteParser.SyncItem(OneNote, item);

        /// <inheritdoc cref="OneNoteParser.GetPageContent(IApplication, OneNotePage)"/>
        public static string GetPageContent(OneNotePage page)=> OneNoteParser.GetPageContent(OneNote, page);

        /// <inheritdoc cref="OneNoteParser.FindPages(IApplication, string)"/>
        public static IEnumerable<OneNotePage> FindPages(string search) => OneNoteParser.FindPages(OneNote, search);

        /// <inheritdoc cref="OneNoteParser.FindPages(IApplication, string, IOneNoteItem)"/>
        public static IEnumerable<OneNotePage> FindPages(IOneNoteItem scope, string search) => OneNoteParser.FindPages(OneNote, search, scope);

        /// <inheritdoc cref="OneNoteParser.GetDefaultNotebookLocation(IApplication)"/>
        public static string GetDefaultNotebookLocation() => OneNoteParser.GetDefaultNotebookLocation(OneNote);

        /// <inheritdoc cref="OneNoteParser.CreateQuickNote(IApplication, bool)"/>
        public static void CreateQuickNote() => OneNoteParser.CreateQuickNote(OneNote, true);

        /// <inheritdoc cref="OneNoteParser.CreatePage(IApplication, OneNoteSection, string, bool)"/>
        public static void CreatePage(OneNoteSection section, string name) => OneNoteParser.CreatePage(OneNote, section, name, true);

        /// <inheritdoc cref="OneNoteParser.CreateSection(IApplication, OneNoteSectionGroup, string, bool)"/>
        public static void CreateSection(OneNoteSectionGroup parent, string name) => OneNoteParser.CreateSection(OneNote, parent, name, true);

        /// <inheritdoc cref="OneNoteParser.CreateSection(IApplication, OneNoteNotebook, string, bool)"/>
        public static void CreateSection(OneNoteNotebook parent, string name) => OneNoteParser.CreateSection(OneNote, parent, name, true);

        /// <inheritdoc cref="OneNoteParser.CreateSectionGroup(IApplication, OneNoteSectionGroup, string, bool)"/>
        public static void CreateSectionGroup(OneNoteSectionGroup parent, string name) => OneNoteParser.CreateSectionGroup(OneNote, parent, name, true);

        /// <inheritdoc cref="OneNoteParser.CreateSectionGroup(IApplication, OneNoteNotebook, string, bool)"/>
        public static void CreateSectionGroup(OneNoteNotebook parent, string name) => OneNoteParser.CreateSectionGroup(OneNote, parent, name, true);

        /// <inheritdoc cref="OneNoteParser.CreateNotebook(IApplication, string, bool)"/>
        public static void CreateNotebook(string notebookName) => OneNoteParser.CreateNotebook(OneNote, notebookName, true);

        /// <summary>
        /// Releases the <see cref="Application">OneNote COM object</see> freeing memory.
        /// </summary>
        /// <seealso cref="Init"/>
        /// <seealso cref="HasComObject"/>
        public static void ReleaseComObject()
        {
            if (HasComObject)
            {
                Marshal.ReleaseComObject(OneNote);
                lazyOneNote = new(() => new Application(), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }
}
