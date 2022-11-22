using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using static Flow.Launcher.Plugin.OneNote.ScipBeUtils.Utils;

namespace Flow.Launcher.Plugin.OneNote
{
    public static class Extensions
    {
        
        public static void Sync(this IOneNotePage item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNotePage> items)
        {
            CallOneNoteSafely(onenote => 
            {
                foreach(var item in items)
                {
                    onenote.SyncHierarchy(item.ID); 
                }
            });
        }
        
        public static void Sync(this IOneNoteSection item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNoteSection> items)
        {
            CallOneNoteSafely(onenote => 
            {
                foreach(var item in items)
                {
                    onenote.SyncHierarchy(item.ID); 
                }
            });
        }
        
        public static void Sync(this IOneNoteNotebook item)
        {
            CallOneNoteSafely(onenote => onenote.SyncHierarchy(item.ID));
        }
        public static void Sync(this IEnumerable<IOneNoteNotebook> items)
        {
            CallOneNoteSafely(onenote => 
            {
                foreach(var item in items)
                {
                    onenote.SyncHierarchy(item.ID); 
                }
            });
        }
    }    
}
