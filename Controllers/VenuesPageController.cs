using Azure;
using Booking_webapp.Data;
using Booking_webapp.Models.Entities;
using Booking_webapp.Models.ViewModels;
using Booking_webapp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("Venues")]
    public class VenuesPageController : Controller
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

        private readonly ApplicationDbContext _context;
        private readonly IBlobImageStorageService _blobImageStorageService;

        public VenuesPageController(ApplicationDbContext context, IBlobImageStorageService blobImageStorageService)
        {
            _context = context;
            _blobImageStorageService = blobImageStorageService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm = null)
        {
            var venueQuery = _context.Venues.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                venueQuery = venueQuery.Where(v =>
                    EF.Functions.ILike(v.Name, $"%{term}%") ||
                    EF.Functions.ILike(v.Location, $"%{term}%"));
            }

            var bookingCounts = await _context.Bookings
                .AsNoTracking()
                .GroupBy(b => b.VenueId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count);

            var venues = await venueQuery
                .OrderBy(v => v.Name)
                .Select(venue => new VenueListItemViewModel
                {
                    Id = venue.Id,
                    Name = venue.Name,
                    Location = venue.Location,
                    Capacity = venue.Capacity,
                    ImageUrl = venue.ImageUrl
                })
                .ToListAsync();

            foreach (var venue in venues)
            {
                venue.BookingCount = bookingCounts.GetValueOrDefault(venue.Id);
                venue.ImageUrl = ResolveVenueImageUrl(venue.ImageUrl);
            }

            return View("~/Views/Venues/Index.cshtml", new VenueDirectoryViewModel
            {
                SearchTerm = searchTerm,
                Venues = venues
            });
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Venues/Create.cshtml", new VenueFormViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VenueFormViewModel model)
        {
            ValidateImage(model.ImageFile, nameof(model.ImageFile));

            if (!ModelState.IsValid)
            {
                return View("~/Views/Venues/Create.cshtml", model);
            }

            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Location = model.Location,
                Capacity = model.Capacity
            };

            try
            {
                if (model.ImageFile != null)
                {
                    venue.ImageUrl = await _blobImageStorageService.UploadVenueImageAsync(model.ImageFile);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Venues/Create.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The venue image could not be uploaded right now. Please try again.");
                return View("~/Views/Venues/Create.cshtml", model);
            }

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Venue added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var venue = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

            if (venue == null)
            {
                TempData["ErrorMessage"] = "The selected venue could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var relatedBookings = await (
                from booking in _context.Bookings.AsNoTracking()
                join evnt in _context.Events.AsNoTracking() on booking.EventId equals evnt.Id into eventJoin
                from evnt in eventJoin.DefaultIfEmpty()
                where booking.VenueId == id
                orderby booking.BookingDate descending
                select new RelatedBookingViewModel
                {
                    Id = booking.Id,
                    EventName = evnt != null ? evnt.Name : "Unknown event",
                    BookingDate = booking.BookingDate,
                    Status = booking.Status
                })
                .Take(5)
                .ToListAsync();

            var model = new VenueDetailsViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Location = venue.Location,
                Capacity = venue.Capacity,
                ImageUrl = ResolveVenueImageUrl(venue.ImageUrl),
                BookingCount = await _context.Bookings.CountAsync(b => b.VenueId == id),
                RelatedBookings = relatedBookings
            };

            return View("~/Views/Venues/Details.cshtml", model);
        }

        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);

            if (venue == null)
            {
                TempData["ErrorMessage"] = "The selected venue could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Venues/Edit.cshtml", new VenueFormViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Location = venue.Location,
                Capacity = venue.Capacity,
                ImageUrl = ResolveVenueImageUrl(venue.ImageUrl)
            });
        }

        [HttpPost("Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, VenueFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            ValidateImage(model.ImageFile, nameof(model.ImageFile));

            if (!ModelState.IsValid)
            {
                model.ImageUrl = ResolveVenueImageUrl(model.ImageUrl);
                return View("~/Views/Venues/Edit.cshtml", model);
            }

            var venue = await _context.Venues.FindAsync(id);

            if (venue == null)
            {
                TempData["ErrorMessage"] = "The selected venue could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var previousImage = venue.ImageUrl;

            venue.Name = model.Name;
            venue.Location = model.Location;
            venue.Capacity = model.Capacity;

            try
            {
                if (model.ImageFile != null)
                {
                    venue.ImageUrl = await _blobImageStorageService.UploadVenueImageAsync(model.ImageFile);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.ImageUrl = ResolveVenueImageUrl(previousImage);
                return View("~/Views/Venues/Edit.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The venue image could not be uploaded right now. Please try again.");
                model.ImageUrl = ResolveVenueImageUrl(previousImage);
                return View("~/Views/Venues/Edit.cshtml", model);
            }

            await _context.SaveChangesAsync();

            if (model.ImageFile != null && !string.IsNullOrWhiteSpace(previousImage) && previousImage != venue.ImageUrl)
            {
                await TryDeleteVenueImageAsync(previousImage);
            }

            TempData["SuccessMessage"] = "Venue updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var venue = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

            if (venue == null)
            {
                TempData["ErrorMessage"] = "The selected venue could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Venues/Delete.cshtml", new VenueDetailsViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Location = venue.Location,
                Capacity = venue.Capacity,
                ImageUrl = ResolveVenueImageUrl(venue.ImageUrl),
                BookingCount = await _context.Bookings.CountAsync(b => b.VenueId == id)
            });
        }

        [HttpPost("Delete/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);

            if (venue == null)
            {
                TempData["ErrorMessage"] = "The selected venue could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);

            if (hasBookings)
            {
                TempData["ErrorMessage"] = "This venue cannot be deleted because it is linked to existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            var imageReference = venue.ImageUrl;
            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            await TryDeleteVenueImageAsync(imageReference);

            TempData["SuccessMessage"] = "Venue deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateImage(IFormFile? imageFile, string modelKey)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return;
            }

            if (imageFile.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError(modelKey, "Please upload an image smaller than 5 MB.");
            }

            if (!AllowedImageTypes.Contains(imageFile.ContentType))
            {
                ModelState.AddModelError(modelKey, "Please upload a JPG, PNG, WEBP, or GIF image.");
            }
        }

        private string? ResolveVenueImageUrl(string? storedImage)
        {
            if (string.IsNullOrWhiteSpace(storedImage))
            {
                return null;
            }

            if (Uri.TryCreate(storedImage, UriKind.Absolute, out _))
            {
                return storedImage;
            }

            return Url.Action("VenueImage", "Media", new { blobName = storedImage }) ?? storedImage;
        }

        private async Task TryDeleteVenueImageAsync(string? storedImage)
        {
            try
            {
                await _blobImageStorageService.DeleteVenueImageAsync(storedImage);
            }
            catch
            {
                // Keep the main CRUD flow successful even if blob cleanup fails.
            }
        }
    }
}
