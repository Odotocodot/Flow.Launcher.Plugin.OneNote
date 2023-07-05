using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    public record ActionKeyword
    { 
        [JsonInclude]
        public static readonly ActionKeyword NotebookExplorer = new (0, "Notebook Explorer", $"nb:{Keywords.NotebookExplorerSeparator}", false);
        public static readonly ActionKeyword RecentPages = new (1, "Recent Pages", "rcntpgs:", false);
        public static readonly ActionKeyword TitleSearch = new (2, "Search by title", "*", false, false);
        public static readonly ActionKeyword ScopedSearch = new(3, "Search in a scope", ">", false, false);

        private static readonly Lazy<ActionKeyword[]> actionKeywords = new Lazy<ActionKeyword[]>(() 
            => new ActionKeyword[] {NotebookExplorer,RecentPages,TitleSearch,ScopedSearch });
        public static readonly IEnumerable<ActionKeyword> ActionKeywords = actionKeywords.Value;
        
        public ActionKeyword(int id, string name, string keyword, bool enableInFlowSearch, bool canBeInFlowSearch = true)
        {
            Id = id;
            Name = name;
            Keyword = keyword;
            EnableInFlowSearch = enableInFlowSearch;
            CanBeInFlowSearch = canBeInFlowSearch;
        }

        public int Id { get; init; }
        public string Name { get; init; }
        public string Keyword { get; set; }
        public bool EnableInFlowSearch { get; set; }
        [JsonIgnore]
        public bool CanBeInFlowSearch { get; init; }
    }


}
