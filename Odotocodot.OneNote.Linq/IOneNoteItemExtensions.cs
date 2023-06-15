using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        {
            return items.SelectMany(item => item.Traverse(predicate));
        }
        public static IEnumerable<IOneNoteItem> Traverse(this IEnumerable<IOneNoteItem> items)
        {
            return items.SelectMany(item => item.Traverse());
        }

        /// <summary>
        /// Returns true if the item is a deleted page, deleted section, recycle bin section group, or deleted pages section.
        /// </summary>
        /// <param name="item"></param>
        public static bool IsInRecycleBin(this IOneNoteItem item)
        {
            return item.ItemType switch
            {
                OneNoteItemType.SectionGroup =>  ((OneNoteSectionGroup)item).IsRecycleBin,
                OneNoteItemType.Section => ((OneNoteSection)item).IsInRecycleBin || ((OneNoteSection)item).IsDeletedPages, //If IsDeletedPages is true IsInRecycleBin is always true
                OneNoteItemType.Page => ((OneNotePage)item).IsInRecycleBin,
                _ => false,
            };
        }

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
        public static IEnumerable<OneNotePage> GetPages(this IEnumerable<IOneNoteItem> items)
        {
            return items.Traverse(item => item.ItemType == OneNoteItemType.Page)
                        .Cast<OneNotePage>();
        }
        public static IEnumerable<OneNotePage> GetPages(this IOneNoteItem item)
        {
            return item.Traverse(item => item.ItemType == OneNoteItemType.Page)
                       .Cast<OneNotePage>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The path of the item relative to and inclusive of its notebook</returns>
        public static string GetRelativePath(this IOneNoteItem item, string separator = "\\")
        {
            StringBuilder sb = new(item.Name);
            while(item.Parent != null)
            {
                sb.Insert(0, separator);
                item = item.Parent;
                sb.Insert(0, item.Name);
            }
            return sb.ToString();
        }
    }
}
