﻿using System;
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

        public static void OpenInOneNote(this IOneNoteItem item) => OneNoteApplication.OpenInOneNote(item);

        public static void Sync(this IOneNoteItem item) => OneNoteApplication.SyncItem(item);

        public static string GetPageContent(this OneNotePage page) => OneNoteApplication.GetPageContent(page);

        /// <summary>
        /// Returns true if the item is a deleted page, deleted section, recycle bin section group, or deleted pages section.
        /// </summary>
        /// <param name="item"></param>
        public static bool IsInRecycleBin(this IOneNoteItem item) => item switch
        {
            OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin,
            OneNoteSection section => section.IsInRecycleBin || section.IsDeletedPages, //If IsDeletedPages is true IsInRecycleBin is always true
            OneNotePage page => page.IsInRecycleBin,
            _ => false,
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notebook"></param>
        /// <param name="sectionGroup"></param>
        /// <returns></returns>
        public static bool GetRecycleBin(this OneNoteNotebook notebook, out OneNoteSectionGroup sectionGroup)
        {
            sectionGroup = notebook.SectionGroups.FirstOrDefault(sg => sg.IsRecycleBin);
            return sectionGroup != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns>
        /// A flattened collection of only pages.
        /// </returns>
        public static IEnumerable<OneNotePage> GetPages(this IEnumerable<IOneNoteItem> items) => items.Traverse().OfType<OneNotePage>();
        public static IEnumerable<OneNotePage> GetPages(this IOneNoteItem item) => item.Traverse().OfType<OneNotePage>();
    }
}
