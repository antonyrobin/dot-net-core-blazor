using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BlazorApp.Services.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BlazorApp.Services.Implementations
{
    public class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "uploads";
    }

    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IOptions<AzureBlobOptions> options)
        {
            var opt = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(opt.ConnectionString))
                throw new InvalidOperationException("Azure Blob connection string missing.");

            var blobService = new BlobServiceClient(opt.ConnectionString);
            _containerClient = blobService.GetBlobContainerClient(opt.ContainerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob); // or Private + use SAS
        }

        public async Task<string> StoreAsync(IBrowserFile file, string? directory = null)
        {
            if (file == null || file.Size == 0) return string.Empty;

            string safeDir = string.IsNullOrWhiteSpace(directory)
                ? string.Empty
                : directory.Trim('/').Replace('\\', '/').Replace("..", "");

            string prefix = string.IsNullOrEmpty(safeDir) ? "" : safeDir + "/";
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            string blobName = fileName;           // this is what you return / save in DB

            const long maxSize = 1 * 1024 * 1024;

            // ──────────────────────────────────────────────
            // Read entire file into memory **once**
            // ──────────────────────────────────────────────
            await using var originalStream = file.OpenReadStream(maxAllowedSize: maxSize);
            await using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;   // safe – MemoryStream supports seeking

            bool isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

            var headers = new BlobHttpHeaders { ContentType = file.ContentType };

            if (isImage)
            {
                // Load image from memory stream copy
                using var image = await Image.LoadAsync(memoryStream);

                // Upload ORIGINAL
                memoryStream.Position = 0;   // reset – allowed on MemoryStream
                var originalBlob = _containerClient.GetBlobClient(prefix + fileName);
                await originalBlob.UploadAsync(memoryStream, httpHeaders: headers);

                // Create & upload THUMBNAIL
                string thumbBlobName = prefix + "thumbnail/" + fileName;
                var thumbBlob = _containerClient.GetBlobClient(thumbBlobName);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(300, 300),
                    Mode = ResizeMode.Max
                }));

                await using var thumbStream = new MemoryStream();
                await image.SaveAsync(thumbStream, new JpegEncoder { Quality = 75 });
                thumbStream.Position = 0;

                var thumbHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" };
                await thumbBlob.UploadAsync(thumbStream, httpHeaders: thumbHeaders);
            }
            else
            {
                // Non-image: just upload from memory
                memoryStream.Position = 0;
                var blob = _containerClient.GetBlobClient(prefix + fileName);
                await blob.UploadAsync(memoryStream, httpHeaders: headers);
            }

            return blobName;
        }

        public string GetBlobUrl(string blobName, string? directory = null)
        {
            if (string.IsNullOrEmpty(blobName)) return string.Empty;
            if (!string.IsNullOrEmpty(directory))
            {
                blobName = Path.Combine(directory, blobName);
            }
            var blobClient = _containerClient.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }

        public async Task<string> GetSasUrlAsync(string blobName, string? directory = null, TimeSpan? validity = null)
        {
            if (string.IsNullOrEmpty(blobName)) return string.Empty;
            if (!string.IsNullOrEmpty(directory))
            {
                blobName = Path.Combine(directory, blobName);
            }

            var blobClient = _containerClient.GetBlobClient(blobName);

            // Default: 1 hour
            var expiresOn = DateTimeOffset.UtcNow.Add(validity ?? TimeSpan.FromHours(1));

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = expiresOn,
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        // Add this method to AzureBlobStorageService
        public async Task<(string BlobName, string SasUrl)> GetUploadSasAsync(
            string originalFileName,
            string? directory = null,
            TimeSpan? validity = null)
        {
            string safeDir = string.IsNullOrWhiteSpace(directory)
                ? string.Empty
                : directory.Trim('/').Replace('\\', '/').Replace("..", "");

            string prefix = string.IsNullOrEmpty(safeDir) ? "" : safeDir + "/";
            string extension = Path.GetExtension(originalFileName);
            string uniqueBlobName = $"{Guid.NewGuid()}{extension}";
            string fullBlobName = prefix + uniqueBlobName;

            var blobClient = _containerClient.GetBlobClient(fullBlobName);

            validity ??= TimeSpan.FromMinutes(15); // enough for upload + retries

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fullBlobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow + validity.Value,
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return (uniqueBlobName, sasUri.ToString());
        }
    }
}