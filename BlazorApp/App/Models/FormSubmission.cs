using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace BlazorApp.Models
{
    public class FormSubmission
    {
        [JsonProperty("id")]                 // Newtonsoft.Json
        [JsonPropertyName("id")]             // System.Text.Json
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("_id")]              // Newtonsoft
        [JsonPropertyName("_id")]          // System.Text.Json
        public string PartitionKeyId
        {
            get => Id;
            set => Id = value ?? Id;
        }

        public Dictionary<string, object> TextData { get; set; } = new();
        public Dictionary<string, string> FileData { get; set; } = new();
    }
}
