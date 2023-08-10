using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public partial class KeywordViewModel
    {
        public Keywords Obj { get; init; }
        public PropertyInfo PropertyInfo { get; init; }
        public string Name { get; init; }

        public string Keyword
        {
            get => (string)PropertyInfo.GetValue(Obj);
            set => PropertyInfo.SetValue(Obj, value);
        }

        public static KeywordViewModel[] GetKeywordModels(Keywords keywords)
        {
            return typeof(Keywords).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                   .Select(p => new KeywordViewModel
                                   {
                                       Obj = keywords,
                                       PropertyInfo = p,
                                       Name = NicfyVariableName().Replace(p.Name, " $1"),
                                   })
                                   .ToArray();
        }

        [GeneratedRegex("(\\B[A-Z])")]
        private static partial Regex NicfyVariableName();
    }
}
