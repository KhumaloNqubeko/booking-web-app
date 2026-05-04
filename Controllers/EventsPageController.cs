using Azure;
using Booking_webapp.Data;
using Booking_webapp.Models.Entities;
using Booking_webapp.Models.ViewModels;
using Booking_webapp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("Events")]
    public class EventsPageController : Controller
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

        private readonly ApplicationDbContext _context;
        private readonly IBlobImageStorageService _blobImageStorageService;

        public EventsPageController(ApplicationDbContext context, IBlobImageStorageService blobImageStorageService)
        {
            _context = context;
            _blobImageStorageService = blobImageStorageService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm = null, DateTime? startFrom = null)
        {
            var eventQuery = _context.Events.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                eventQuery = eventQuery.Where(e =>
                    EF.Functions.ILike(e.Name, $"%{term}%") ||
                    EF.Functions.ILike(e.Description, $"%{term}%"));
            }

            if (startFrom.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.StartDateTime.Date >= startFrom.Value.Date);
            }

            var bookingCounts = await _context.Bookings
                .AsNoTracking()
                .GroupBy(b => b.EventId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count);

            var events = await eventQuery
                .OrderBy(e => e.StartDateTime)
                .Select(evnt => new EventListItemViewModel
                {
                    Id = evnt.Id,
                    Name = evnt.Name,
                    Description = evnt.Description,
                    ImageUrl = evnt.ImageUrl,
                    StartDateTime = evnt.StartDateTime,
                    EndDateTime = evnt.EndDateTime
                })
                .ToListAsync();

            foreach (var evnt in events)
            {
                evnt.BookingCount = bookingCounts.GetValueOrDefault(evnt.Id);
                evnt.ImageUrl = ResolveEventImageUrl(evnt.ImageUrl);
            }

            return View("~/Views/Events/Index.cshtml", new EventDirectoryViewModel
            {
                SearchTerm = searchTerm,
                StartFrom = startFrom,
                Events = events
            });
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Events/Create.cshtml", new EventFormViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            ValidateEventDates(model);
            ValidateImage(model.ImageFile, nameof(model.ImageFile));

            if (!ModelState.IsValid)
            {
                return View("~/Views/Events/Create.cshtml", model);
            }

            var evnt = new Event
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                StartDateTime = model.StartDateTime,
                EndDateTime = model.EndDateTime
            };

            try
            {
                if (model.ImageFile != null)
                {
                    evnt.ImageUrl = await _blobImageStorageService.UploadEventImageAsync(model.ImageFile);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Events/Create.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The event image could not be uploaded right now. Please try again.");
                return View("~/Views/Events/Create.cshtml", model);
            }

            _context.Events.Add(evnt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var evnt = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var relatedBookings = await (
                from booking in _context.Bookings.AsNoTracking()
                join venue in _context.Venues.AsNoTracking() on booking.VenueId equals venue.Id into venueJoin
                from venue in venueJoin.DefaultIfEmpty()
                where booking.EventId == id
                orderby booking.BookingDate descending
                select new EventBookingViewModel
                {
                    Id = booking.Id,
                    VenueName = venue != null ? venue.Name : "Unknown venue",
                    BookingDate = booking.BookingDate,
                    Status = booking.Status
                })
                .Take(5)
                .ToListAsync();

            var model = new EventDetailsViewModel
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Description = evnt.Description,
                ImageUrl = ResolveEventImageUrl(evnt.ImageUrl),
                StartDateTime = evnt.StartDateTime,
                EndDateTime = evnt.EndDateTime,
                BookingCount = await _context.Bookings.CountAsync(b => b.EventId == id),
                RelatedBookings = relatedBookings
            };

            return View("~/Views/Events/Details.cshtml", model);
        }

        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var evnt = await _context.Events.FindAsync(id);

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Events/Edit.cshtml", new EventFormViewModel
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Description = evnt.Description,
                ImageUrl = ResolveEventImageUrl(evnt.ImageUrl),
                StartDateTime = evnt.StartDateTime,
                EndDateTime = evnt.EndDateTime
            });
        }

        [HttpPost("Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EventFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            ValidateEventDates(model);
            ValidateImage(model.ImageFile, nameof(model.ImageFile));

            if (!ModelState.IsValid)
            {
                model.ImageUrl = ResolveEventImageUrl(model.ImageUrl);
                return View("~/Views/Events/Edit.cshtml", model);
            }

            var evnt = await _context.Events.FindAsync(id);

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var previousImage = evnt.ImageUrl;

            evnt.Name = model.Name;
            evnt.Description = model.Description;
            evnt.StartDateTime = model.StartDateTime;
            evnt.EndDateTime = model.EndDateTime;

            try
            {
                if (model.ImageFile != null)
                {
                    evnt.ImageUrl = await _blobImageStorageService.UploadEventImageAsync(model.ImageFile);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.ImageUrl = ResolveEventImageUrl(previousImage);
                return View("~/Views/Events/Edit.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The event image could not be uploaded right now. Please try again.");
                model.ImageUrl = ResolveEventImageUrl(previousImage);
                return View("~/Views/Events/Edit.cshtml", model);
            }

            await _context.SaveChangesAsync();

            if (model.ImageFile != null && !string.IsNullOrWhiteSpace(previousImage) && previousImage != evnt.ImageUrl)
            {
                await TryDeleteEventImageAsync(previousImage);
            }

            TempData["SuccessMessage"] = "Event updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var evnt = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Events/Delete.cshtml", new EventDetailsViewModel
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Description = evnt.Description,
                ImageUrl = ResolveEventImageUrl(evnt.ImageUrl),
                StartDateTime = evnt.StartDateTime,
                EndDateTime = evnt.EndDateTime,
                BookingCount = await _context.Bookings.CountAsync(b => b.EventId == id)
            });
        }

        [HttpPost("Delete/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var evnt = await _context.Events.FindAsync(id);

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);

            if (hasBookings)
            {
                TempData["ErrorMessage"] = "This event cannot be deleted because it is linked to existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            var imageReference = evnt.ImageUrl;
            _context.Events.Remove(evnt);
            await _context.SaveChangesAsync();
            await TryDeleteEventImageAsync(imageReference);

            TempData["SuccessMessage"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateEventDates(EventFormViewModel model)
        {
            if (model.EndDateTime <= model.StartDateTime)
            {
                ModelState.AddModelError(string.Empty, "The event end time must be later than the start time.");
            }
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

        private string? ResolveEventImageUrl(string? storedImage)
        {
            if (string.IsNullOrWhiteSpace(storedImage))
            {
                return null;
            }

            if (Uri.TryCreate(storedImage, UriKind.Absolute, out _))
            {
                return storedImage;
            }

            return Url.Action("EventImage", "Media", new { blobName = storedImage }) ?? storedImage;
        }

        private async Task TryDeleteEventImageAsync(string? storedImage)
        {
            try
            {
                await _blobImageStorageService.DeleteEventImageAsync(storedImage);
            }
            catch
            {
                // Keep the main CRUD flow successful even if blob cleanup fails.
            }
        }
    }
}
