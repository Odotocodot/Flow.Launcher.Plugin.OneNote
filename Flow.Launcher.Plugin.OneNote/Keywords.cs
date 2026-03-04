using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    
    public class Keywords
    {
        public const string NotebookExplorerSeparator = "\\";
        public Keyword NotebookExplorer { get; set; } = new($"nb:{NotebookExplorerSeparator}");
        public Keyword RecentPages { get; set; } = new ("rp:");
        public Keyword TitleSearch { get; set; } = new ("*");
        public Keyword ScopedSearch { get; set; } = new (">");
        
    }
    
    [JsonConverter(typeof(KeywordJsonConverter))]
    public class Keyword
    {
        public Keyword(string value) => Value = value;
        public string Value { get; private set; }

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