using Microsoft.AspNetCore.Components.Forms;

namespace BlazorApp.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> StoreAsync(IBrowserFile file, string? directory = null);
        string GetBlobUrl(string blobName, string? directory = null);
        Task<string> GetSasUrlAsync(string blobName, string? directory = null, TimeSpan? validity = null);
    }
}
