using BlazorApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorApp.Services.Interfaces
{
    public interface IFormSubmissionService
    {
        Task SaveAsync(Dictionary<string, string> textData, Dictionary<string, IBrowserFile> files);
        Task UpdateAsync(string id, Dictionary<string, string> textData, Dictionary<string, IBrowserFile> files);
        Task<List<FormSubmission>> GetAllAsync();
        Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken);
        Task<FormSubmission?> GetByIdAsync(string id);
        Task DeleteAsync(string id);
    }
}
