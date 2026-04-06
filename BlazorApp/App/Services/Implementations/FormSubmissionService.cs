using BlazorApp.Models;
using BlazorApp.Repositories.Interfaces;
using BlazorApp.Services.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorApp.Services.Implementations
{
    public class FormSubmissionService : IFormSubmissionService
    {
        private readonly IFormSubmissionRepository _repository;
        private readonly IFileStorageService _fileStorage;

        public FormSubmissionService(IFormSubmissionRepository repository, IFileStorageService fileStorage)
        {
            _repository = repository;
            _fileStorage = fileStorage;
        }

        public async Task SaveAsync(Dictionary<string, string> textData, Dictionary<string, IBrowserFile> files)
        {
            var fileData = new Dictionary<string, string>();

            foreach (var kvp in files)
            {
                if (kvp.Value != null && kvp.Value.Size > 0)
                {
                    string storedFileName = await _fileStorage.StoreAsync(kvp.Value, kvp.Key);
                    if (!string.IsNullOrEmpty(storedFileName))
                    {
                        fileData[kvp.Key] = storedFileName;
                    }
                }
            }

            var submission = new FormSubmission
            {
                TextData = textData.ToDictionary(k => k.Key, v => (object)v.Value),
                FileData = fileData
            };

            await _repository.SaveAsync(submission);
        }

        public async Task UpdateAsync(string id, Dictionary<string, string> textData, Dictionary<string, IBrowserFile> files)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return;

            // Update text data
            existing.TextData.Clear();
            foreach (var kvp in textData)
            {
                existing.TextData[kvp.Key] = kvp.Value;
            }

            // Update files (only if new file uploaded)
            foreach (var kvp in files)
            {
                if (kvp.Value != null && kvp.Value.Size > 0)
                {
                    string storedFileName = await _fileStorage.StoreAsync(kvp.Value, kvp.Key);
                    if (!string.IsNullOrEmpty(storedFileName))
                    {
                        existing.FileData[kvp.Key] = storedFileName;
                    }
                }
            }

            await _repository.UpdateAsync(existing);
        }

        public async Task<List<FormSubmission>> GetAllAsync()
            => await _repository.GetAllAsync();

        public async Task<PagedResult<FormSubmission>> GetPagedAsync(string? search, int pageSize, string? continuationToken)
            => await _repository.GetPagedAsync(search, pageSize, continuationToken);

        public async Task<FormSubmission?> GetByIdAsync(string id)
            => await _repository.GetByIdAsync(id);

        public async Task DeleteAsync(string id)
            => await _repository.DeleteAsync(id);
    }
}
