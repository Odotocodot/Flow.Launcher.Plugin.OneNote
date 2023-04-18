using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Odotocodot.OneNote.Linq
{
    public static class Util
    {
        /// <summary>
        /// Do retry and return some value
        /// </summary>
        /// <typeparam name="TRet">Return type</typeparam>
        /// <typeparam name="TException">Type of exception to catch</typeparam>
        /// <param name="action">Action to perform</param>
        /// <param name="retryInterval">Interval between retries</param>
        /// <param name="retryCount">How many times to retry</param>
        /// <param name="onRetry">Action to call on retry</param>
        /// <returns>Action return</returns>
        public static TRet TryCatchAndRetry<TRet, TException>(Func<TRet> action, TimeSpan retryInterval, int retryCount, Action<TException> onRetry = null)
            where TException : Exception
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return action();
                }
                catch (TException ex)
                {
                    if (attempt++ < retryCount)
                    {
                        onRetry(ex);
                        Thread.Sleep(retryInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

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
    }
}
