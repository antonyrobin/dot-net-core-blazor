using BlazorApp.Models;

namespace BlazorApp.Repositories.Interfaces
{
    public interface IFormSubmissionRepository
    {
        Task<List<FormSubmission>> GetAllAsync();
        Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken);
        Task<FormSubmission?> GetByIdAsync(string id);
        Task SaveAsync(FormSubmission submission);
        Task UpdateAsync(FormSubmission submission);
        Task DeleteAsync(string id);
    }
}
