using System;
using System.Collections.Generic;
using System.Linq;

namespace Odotocodot.OneNote.Linq
{
    public interface IOneNoteItem
    {
        /// <summary>
        /// ID of the OneNote item.
        /// </summary>
        string ID { get; }
        /// <summary>
        /// Name of the OneNote item.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Whether the item has unread information.
        /// </summary>
        bool IsUnread { get; }
        /// <summary>
        /// The path of the item relative to and inclusive of its notebook 
        /// </summary>
        string RelativePath { get; }
        /// <summary>
        /// The OneNote item type, i.e., whether it is a notebook, section group, section or page.
        /// </summary>
        OneNoteItemType ItemType { get; }

        /// <summary>
        /// The children of the item, e.g. for a notebook it would be sections and/or section groups.
        /// </summary>
        /// <returns>
        /// <see cref="Enumerable.Empty{IOneNoteItem}()"/> if the item has no children.
        /// </returns>
        IEnumerable<IOneNoteItem> Children { get; }

        /// <summary>
        /// Depth first traversal
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IOneNoteItem> Traverse(Func<IOneNoteItem, bool> predicate)
        {
            var stack = new Stack<IOneNoteItem>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (predicate(current))
                    yield return current;

                foreach (var child in current.Children)
                    stack.Push(child);
            }
        }
        public IEnumerable<IOneNoteItem> Traverse()
        {
            var stack = new Stack<IOneNoteItem>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                yield return current;

                foreach (var child in current.Children)
                    stack.Push(child);
            }
        }
    }
}
