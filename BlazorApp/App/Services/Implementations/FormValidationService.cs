using System.Text.RegularExpressions;
using BlazorApp.Models;
using BlazorApp.Services.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorApp.Services.Implementations
{
    public class FormValidationService : IFormValidationService
    {
        public Dictionary<string, string> Validate(Dictionary<string, string> formData, Dictionary<string, IBrowserFile> files, List<FormControl> controls, bool isEdit)
        {
            var errors = new Dictionary<string, string>();

            foreach (var control in controls)
            {
                string name = control.Name;

                // === FILE / IMAGE VALIDATION ===
                if (control.Type == "image" || control.Type == "file")
                {
                    bool hasNewFile = files.ContainsKey(name) && files[name]?.Size > 0 == true;

                    if (control.Mandatory && !hasNewFile && !isEdit)
                    {
                        errors[name] = $"{control.Label} is required.";
                    }
                    else if (hasNewFile && control.Type == "image")
                    {
                        var file = files[name]!;
                        if (!file.ContentType.StartsWith("image/"))
                        {
                            errors[name] = "Only image files (JPG, PNG, etc.) are allowed.";
                        }
                    }
                    continue;
                }

                // === TEXT FIELD VALIDATION ===
                string value = formData.GetValueOrDefault(name) ?? "";

                if (control.Mandatory && string.IsNullOrWhiteSpace(value))
                {
                    errors[name] = $"{control.Label} is required.";
                }
                else if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(control.Regex))
                {
                    if (!Regex.IsMatch(value, control.Regex))
                    {
                        errors[name] = control.ValidationMessage ?? "Invalid format.";
                    }
                }
            }
            return errors;
        }
    }
}
