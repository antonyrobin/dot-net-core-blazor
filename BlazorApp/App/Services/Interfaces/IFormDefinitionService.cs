using BlazorApp.Models;

namespace BlazorApp.Services.Interfaces
{
    public interface IFormDefinitionService
    {
        List<FormControl> GetFormControls();
    }
}
