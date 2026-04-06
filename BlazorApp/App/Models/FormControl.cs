namespace BlazorApp.Models
{
    public class FormControl
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
        public List<string> Options { get; set; } = new();
        public string? Regex { get; set; }
        public string? ValidationMessage { get; set; }
    }
}
