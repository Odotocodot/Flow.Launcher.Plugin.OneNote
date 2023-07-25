using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Keyword : UI.Model
    {
        public const string NotebookExplorerSeparator = "\\";

        private string keywordValue;

        [JsonConstructor]
        public Keyword(string name, string keywordValue)
        {
            Name = name;
            KeywordValue = keywordValue;
        }

        [JsonPropertyName("Keyword")]
        public string KeywordValue { get => keywordValue; set => SetProperty(ref keywordValue, value); }
        [JsonIgnore]
        public string Name { get; init; }
        [JsonIgnore]
        public int Length => keywordValue.Length;

        public static implicit operator string(Keyword keyword)
        {
            return keyword.KeywordValue;
        }
        public override string ToString()
        {
            return KeywordValue;
        }
    }
}