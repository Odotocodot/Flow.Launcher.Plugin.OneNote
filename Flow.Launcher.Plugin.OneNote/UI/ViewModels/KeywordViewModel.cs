using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public partial class KeywordViewModel : BaseModel
    {
        private object Instance { get; init; }
        private PropertyInfo PropertyInfo { get; init; }
        public string Name { get; private init; }

        public string Keyword
        {
            get => (string)PropertyInfo.GetValue(Instance);
            set
            {
                PropertyInfo.SetValue(Instance, value, null);
                OnPropertyChanged();
            }
        }


        public static KeywordViewModel[] GetKeywordViewModels(Keywords keywords)
        {
            return keywords.GetType()
                           .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Select(p => new KeywordViewModel
                           {
                               Instance = keywords,
                               PropertyInfo = p,
                               Name = NicfyPropertyName().Replace(p.Name, " $1"),
                           })
                           .ToArray();
        }

        [GeneratedRegex("(\\B[A-Z])")]
        private static partial Regex NicfyPropertyName();
    }
}
