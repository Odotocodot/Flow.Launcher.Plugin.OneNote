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
        /// Returns true if the item is a deleted page or section.<br/> 
        /// If <paramref name="includeSpecialItems"/> is <see langword="true"/>, it also returns <see langword="true"/> if the <paramref name="item"/> is the special section group "OneNote Recycle Bin" or the special section "Deleted Pages".
        /// </summary>
        /// <param name="item"></param>
        /// <param name="includeSpecialItems"></param>
        /// <returns></returns>
        public static bool IsInRecycleBin(this IOneNoteItem item, bool includeSpecialItems = true)
        {
            return item.ItemType switch
            {
                OneNoteItemType.SectionGroup => includeSpecialItems && ((OneNoteSectionGroup)item).IsRecycleBin,
                OneNoteItemType.Section => ((OneNoteSection)item).IsInRecycleBin || (includeSpecialItems && ((OneNoteSection)item).IsDeletedPages),
                OneNoteItemType.Page => ((OneNotePage)item).IsInRecycleBin,
                _ => false,
            };
        }
    }
}
