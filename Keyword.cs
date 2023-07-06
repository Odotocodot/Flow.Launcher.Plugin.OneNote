using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Keyword : UI.Model
    {
        public const string NotebookExplorerSeparator = "\\";

        private bool enableInFlowSearch;
        private string keywordValue;

        [JsonConstructor]
        public Keyword(int id, string name, string keywordValue, bool enableInFlowSearch, bool canBeInFlowSearch = true)
        {
            Id = id;
            Name = name;
            KeywordValue = keywordValue;
            EnableInFlowSearch = enableInFlowSearch;
            CanBeInFlowSearch = canBeInFlowSearch;
        }

        #region Properties
        [JsonPropertyName("Keyword")]
        public string KeywordValue { get => keywordValue; set => SetProperty(ref keywordValue, value); }
        public bool EnableInFlowSearch
        {
            get => enableInFlowSearch; set
            {
                if (CanBeInFlowSearch)
                    SetProperty(ref enableInFlowSearch, value);
                else
                    enableInFlowSearch = false;
            }
        }
        [JsonIgnore]
        public bool CanBeInFlowSearch { get; init; }
        [JsonIgnore]
        public int Id { get; init; }
        [JsonIgnore]
        public string Name { get; init; }
        [JsonIgnore]
        public int Length => keywordValue.Length;

        #endregion

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