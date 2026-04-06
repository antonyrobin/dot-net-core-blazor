using System.IO;
using System.Threading.Tasks;
using BlazorApp.Services.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace BlazorApp.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly string _uploadDirectory = "files";

        public FileStorageService(IWebHostEnvironment env)
        {
            _basePath = Path.Combine(env.WebRootPath, _uploadDirectory);
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> StoreAsync(IBrowserFile file, string? directory = null)
        {
            if (file == null || file.Size == 0) return string.Empty;

            string safeDir = string.IsNullOrWhiteSpace(directory)
                ? string.Empty
                : directory.Trim('/').Replace('\\', '/').Replace("..", "");

            string subPath = string.IsNullOrEmpty(safeDir) ? "" : safeDir + "/";
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            string blobName = fileName;

            string fullPath = Path.Combine(_basePath, subPath + fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            const long maxSize = 10 * 1024 * 1024;

            await using var inputStream = file.OpenReadStream(maxAllowedSize: maxSize);
            await using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            bool isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

            if (isImage)
            {
                using var image = await Image.LoadAsync(memoryStream);

                // Save original
                memoryStream.Position = 0;
                await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await memoryStream.CopyToAsync(fs);
                }

                // Thumbnail
                string thumbDir = Path.Combine(Path.GetDirectoryName(fullPath)!, "thumbnail");
                Directory.CreateDirectory(thumbDir);
                string thumbPath = Path.Combine(thumbDir, fileName);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(300, 300),
                    Mode = ResizeMode.Max
                }));

                await using (var thumbFs = new FileStream(thumbPath, FileMode.Create, FileAccess.Write))
                {
                    await image.SaveAsync(thumbFs, new JpegEncoder { Quality = 75 });
                }
            }
            else
            {
                memoryStream.Position = 0;
                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await memoryStream.CopyToAsync(fs);
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
            return $"/{_uploadDirectory}/{blobName}";
        }

        public Task<string> GetSasUrlAsync(string blobName, string? directory = null, TimeSpan? validity = null)
        {
            // Local filesystem → no real SAS, just return same URL
            // In production you might want different logic (e.g. temporary signed URL via API)
            return Task.FromResult(GetBlobUrl(blobName, directory));
        }

        private static IImageEncoder GetEncoder(Image image)
        {
            return image.Metadata.DecodedImageFormat switch
            {
                var f when f.Name == "JPEG" => new JpegEncoder { Quality = 85 },
                var f when f.Name == "PNG" => new PngEncoder(),
                _ => new JpegEncoder { Quality = 85 }
            };
        }
    }
}