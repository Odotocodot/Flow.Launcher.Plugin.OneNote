using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public static class IOneNoteItemExtensions
    {
        /// <summary>
        /// Depth first traversal
        /// </summary>
        /// <returns></returns>
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
    }
}
