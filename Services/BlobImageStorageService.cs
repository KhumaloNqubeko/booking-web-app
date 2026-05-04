using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Booking_webapp.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Booking_webapp.Services
{
    public class BlobImageStorageService : IBlobImageStorageService
    {
        private readonly AzureBlobStorageOptions _options;

        public BlobImageStorageService(IOptions<AzureBlobStorageOptions> options)
        {
            _options = options.Value;
        }

        public Task<string> UploadVenueImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadAsync(file, _options.VenueContainerName, cancellationToken);
        }

        public Task<string> UploadEventImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadAsync(file, _options.EventContainerName, cancellationToken);
        }

        public Task DeleteVenueImageAsync(string? storedReference, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(storedReference, _options.VenueContainerName, cancellationToken);
        }

        public Task DeleteEventImageAsync(string? storedReference, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(storedReference, _options.EventContainerName, cancellationToken);
        }

        public Task<StoredImageFile?> OpenVenueImageAsync(string storedReference, CancellationToken cancellationToken = default)
        {
            return OpenReadAsync(storedReference, _options.VenueContainerName, cancellationToken);
        }

        public Task<StoredImageFile?> OpenEventImageAsync(string storedReference, CancellationToken cancellationToken = default)
        {
            return OpenReadAsync(storedReference, _options.EventContainerName, cancellationToken);
        }

        private async Task<string> UploadAsync(IFormFile file, string containerName, CancellationToken cancellationToken)
        {
            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType
                }
            }, cancellationToken);

            return blobName;
        }

        private async Task DeleteAsync(string? storedReference, string containerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storedReference) || IsAbsoluteUrl(storedReference))
            {
                return;
            }

            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(storedReference);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }

        private async Task<StoredImageFile?> OpenReadAsync(string storedReference, string containerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storedReference) || IsAbsoluteUrl(storedReference))
            {
                return null;
            }

            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(storedReference);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var downloadResponse = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            return new StoredImageFile
            {
                Content = downloadResponse.Value.Content,
                ContentType = string.IsNullOrWhiteSpace(downloadResponse.Value.Details.ContentType)
                    ? "application/octet-stream"
                    : downloadResponse.Value.Details.ContentType
            };
        }

        private async Task<BlobContainerClient> GetContainerClientAsync(string containerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage is not configured yet. Add the storage connection string before uploading images.");
            }

            var serviceClient = new BlobServiceClient(_options.ConnectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return containerClient;
        }

        private static bool IsAbsoluteUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out _);
        }
    }
}
