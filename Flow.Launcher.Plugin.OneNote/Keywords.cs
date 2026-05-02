using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    
    public class Keywords
    {
        public const string NotebookExplorerSeparator = "\\";
        public Keyword NotebookExplorer { get; init; } = new($"nb:{NotebookExplorerSeparator}");
        [JsonPropertyName("RecentPages")]
        public Keyword SortByLastModified { get; init; } = new ("rp:");
        public Keyword TitleSearch { get; init; } = new ("*");
        public Keyword ScopedSearch { get; init; } = new (">");

        private Keyword[]? keywords;
        [JsonIgnore]
        public Keyword[] All => keywords ??= [NotebookExplorer, SortByLastModified, TitleSearch, ScopedSearch];
    }
    
    [JsonConverter(typeof(KeywordJsonConverter))]
    public class Keyword(string value)
    {
        public string Value { get; private set; } = value;

        public void ChangeKeyword(string newValue) => Value = newValue;

        public int Length => Value.Length;
        public static implicit operator string(Keyword keyword) => keyword.Value; 
        public override string ToString() => Value;
    }
    
    //Needed for legacy as keywords where just saved as a string
    public class KeywordJsonConverter : JsonConverter<Keyword>
    {
        public override Keyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => new(JsonSerializer.Deserialize<string>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, Keyword value, JsonSerializerOptions options) 
            => JsonSerializer.Serialize(writer, value.Value, options);
    }
}