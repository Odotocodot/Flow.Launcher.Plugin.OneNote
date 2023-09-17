using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public static class IOneNoteItemExtensions
    {
        //Depth first traversal 
        public static IEnumerable<IOneNoteItem> Traverse(this IOneNoteItem item, Func<IOneNoteItem, bool> predicate)
        {
            var stack = new Stack<IOneNoteItem>();
            stack.Push(item);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (predicate(current))
                    yield return current;

                foreach (var child in current.Children)
                    stack.Push(child);
            }
        }
        public static IEnumerable<IOneNoteItem> Traverse(this IOneNoteItem item)
        {
            var stack = new Stack<IOneNoteItem>();
            stack.Push(item);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                yield return current;

                foreach (var child in current.Children)
                    stack.Push(child);
            }
        }

        public static IEnumerable<IOneNoteItem> Traverse(this IEnumerable<IOneNoteItem> items, Func<IOneNoteItem, bool> predicate) 
            => items.SelectMany(item => item.Traverse(predicate));
        public static IEnumerable<IOneNoteItem> Traverse(this IEnumerable<IOneNoteItem> items) 
            => items.SelectMany(item => item.Traverse());

        /// <inheritdoc cref="OneNoteParser.OpenInOneNote(Microsoft.Office.Interop.OneNote.IApplication, IOneNoteItem)"/>
        public static void OpenInOneNote(this IOneNoteItem item) => OneNoteApplication.OpenInOneNote(item);

        /// <inheritdoc cref="OneNoteParser.SyncItem(Microsoft.Office.Interop.OneNote.IApplication, IOneNoteItem)"/>
        public static void Sync(this IOneNoteItem item) => OneNoteApplication.SyncItem(item);

        /// <inheritdoc cref="OneNoteParser.GetPageContent(Microsoft.Office.Interop.OneNote.IApplication, OneNotePage)"/>
        public static string GetPageContent(this OneNotePage page) => OneNoteApplication.GetPageContent(page);

        ///// <summary>
        ///// Returns
        ///// </summary>
        ///// <param name="item"></param>
        ///// <returns> <see langword="true"/> if the item is a deleted page <see cref="OneNotePage.IsInRecycleBin"/>, deleted section, recycle bin section group, or deleted pages section.</returns>
        public static bool IsInRecycleBin(this IOneNoteItem item) => item switch
        {
            OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin,
            OneNoteSection section => section.IsInRecycleBin || section.IsDeletedPages, //If IsDeletedPages is true IsInRecycleBin is always true
            OneNotePage page => page.IsInRecycleBin,
            _ => false,
        };

        public static bool GetRecycleBin(this OneNoteNotebook notebook, out OneNoteSectionGroup sectionGroup)
        {
            sectionGroup = notebook.SectionGroups.FirstOrDefault(sg => sg.IsRecycleBin);
            return sectionGroup != null;
        }


        ///// <param name="items">The collection of items to filter get pages from.</param>
        ///// <returns>
        ///// A flattened collection of only the pages nested in <paramref name="items"/>.
        ///// </returns>
        public static IEnumerable<OneNotePage> GetPages(this IEnumerable<IOneNoteItem> items) => (IEnumerable<OneNotePage>)items.Traverse(i => i is OneNotePage);
        ///// <param name="item">The item to filter get pages from.</param>
        ///// <returns>
        ///// A flattened collection of only the pages nested in <paramref name="item"/>.
        ///// </returns>
        public static IEnumerable<OneNotePage> GetPages(this IOneNoteItem item) => (IEnumerable<OneNotePage>)item.Traverse(i => i is OneNotePage);
    }
}
