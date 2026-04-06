using BlazorApp.Models;
using BlazorApp.Services.Interfaces;

namespace BlazorApp.Services.Implementations
{
    public class FormDefinitionService : IFormDefinitionService
    {
        public List<FormControl> GetFormControls() => new()
        {
            new FormControl { Type = "text", Name = "fullName", Label = "Full Name", Mandatory = true },
            new FormControl { Type = "email", Name = "emailAddress", Label = "Email Address", Mandatory = true },
            new FormControl { Type = "number", Name = "experience", Label = "Experience (years)", Mandatory = false },
            new FormControl { Type = "textarea", Name = "summary", Label = "Professional Summary", Mandatory = false },
            new FormControl { Type = "richtext", Name = "coverLetter", Label = "Cover Letter", Mandatory = false },
            new FormControl { Type = "image", Name = "profilePic", Label = "Profile Picture", Mandatory = false },
            new FormControl { Type = "file", Name = "resume", Label = "Resume / Documents", Mandatory = false },
            // Add your select/radio/checkbox controls here...
        };
    }
}
