using Azure;
using Booking_webapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace Booking_webapp.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly IBlobImageStorageService _blobImageStorageService;

        public MediaController(IBlobImageStorageService blobImageStorageService)
        {
            _blobImageStorageService = blobImageStorageService;
        }

        [HttpGet("venues/{*blobName}")]
        public async Task<IActionResult> VenueImage(string blobName, CancellationToken cancellationToken)
        {
            return await StreamImageAsync(
                () => _blobImageStorageService.OpenVenueImageAsync(Uri.UnescapeDataString(blobName), cancellationToken));
        }

        [HttpGet("events/{*blobName}")]
        public async Task<IActionResult> EventImage(string blobName, CancellationToken cancellationToken)
        {
            return await StreamImageAsync(
                () => _blobImageStorageService.OpenEventImageAsync(Uri.UnescapeDataString(blobName), cancellationToken));
        }

        private static async Task<IActionResult> StreamImageAsync(Func<Task<StoredImageFile?>> openStream)
        {
            try
            {
                var image = await openStream();

                if (image == null)
                {
                    return new NotFoundResult();
                }

                return new FileStreamResult(image.Content, image.ContentType);
            }
            catch (RequestFailedException)
            {
                return new NotFoundResult();
            }
        }
    }
}
