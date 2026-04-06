using BlazorApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorApp.Services.Interfaces
{
    public interface IFormValidationService
    {
        Dictionary<string, string> Validate(Dictionary<string, string> formData, Dictionary<string, IBrowserFile> files, List<FormControl> controls, bool isEdit);
    }
}
