using Microsoft.AspNetCore.Http;

namespace Booking_webapp.Services
{
    public interface IBlobImageStorageService
    {
        Task<string> UploadVenueImageAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<string> UploadEventImageAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task DeleteVenueImageAsync(string? storedReference, CancellationToken cancellationToken = default);
        Task DeleteEventImageAsync(string? storedReference, CancellationToken cancellationToken = default);
        Task<StoredImageFile?> OpenVenueImageAsync(string storedReference, CancellationToken cancellationToken = default);
        Task<StoredImageFile?> OpenEventImageAsync(string storedReference, CancellationToken cancellationToken = default);
    }

    public sealed class StoredImageFile
    {
        public required Stream Content { get; init; }
        public required string ContentType { get; init; }
    }
}
