using Azure;
using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Booking_webapp.Models.ViewModels;
using Booking_webapp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            int? eventTypeId = null,
            Guid? venueId = null,
            string? venueAvailability = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var eventQuery =
                from evnt in _context.Events.AsNoTracking()
                join eventType in _context.EventTypes.AsNoTracking() on evnt.EventTypeId equals eventType.Id into eventTypeJoin
                from eventType in eventTypeJoin.DefaultIfEmpty()
                select new
                {
                    Event = evnt,
                    EventTypeName = eventType != null ? eventType.Name : "Unknown type"
                };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                eventQuery = eventQuery.Where(e =>
                    e.Event.Name.ToLower().Contains(term) ||
                    e.Event.Description.ToLower().Contains(term) ||
                    e.EventTypeName.ToLower().Contains(term));
            }

            if (eventTypeId.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.Event.EventTypeId == eventTypeId.Value);
            }

            if (dateFrom.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.Event.StartDateTime.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                eventQuery = eventQuery.Where(e => e.Event.EndDateTime.Date <= dateTo.Value.Date);
            }

            if (venueId.HasValue)
            {
                eventQuery = eventQuery.Where(e =>
                    _context.Bookings.Any(b => b.EventId == e.Event.Id && b.VenueId == venueId.Value));
            }

            if (!string.IsNullOrWhiteSpace(venueAvailability))
            {
                eventQuery = eventQuery.Where(e =>
                    (from booking in _context.Bookings
                     join venue in _context.Venues on booking.VenueId equals venue.Id
                     where booking.EventId == e.Event.Id
                     select venue.Availability)
                    .Any(availability => availability == venueAvailability));
            }

            var bookingCounts = await _context.Bookings
                .AsNoTracking()
                .GroupBy(b => b.EventId)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count);

            var events = await eventQuery
                .OrderBy(e => e.Event.StartDateTime)
                .Select(e => new EventListItemViewModel
                {
                    Id = e.Event.Id,
                    Name = e.Event.Name,
                    Description = e.Event.Description,
                    ImageUrl = e.Event.ImageUrl,
                    EventTypeName = e.EventTypeName,
                    StartDateTime = e.Event.StartDateTime,
                    EndDateTime = e.Event.EndDateTime
                })
                .ToListAsync();

            foreach (var evnt in events)
            {
                evnt.BookingCount = bookingCounts.GetValueOrDefault(evnt.Id);
                evnt.ImageUrl = ResolveEventImageUrl(evnt.ImageUrl);
            }

            var model = new EventDirectoryViewModel
            {
                SearchTerm = searchTerm,
                EventTypeId = eventTypeId,
                VenueId = venueId,
                VenueAvailability = venueAvailability,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Events = events
            };

            await PopulateEventFilterSelectionsAsync(model);
            return View("~/Views/Events/Index.cshtml", model);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new EventFormViewModel();
            await PopulateEventTypeOptionsAsync(model);
            return View("~/Views/Events/Create.cshtml", model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            ValidateEventDates(model);
            ValidateImage(model.ImageFile, nameof(model.ImageFile));
            await ValidateEventTypeAsync(model.EventTypeId);

            if (!ModelState.IsValid)
            {
                await PopulateEventTypeOptionsAsync(model);
                return View("~/Views/Events/Create.cshtml", model);
            }

            var evnt = new Event
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                EventTypeId = model.EventTypeId,
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
                await PopulateEventTypeOptionsAsync(model);
                return View("~/Views/Events/Create.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The event image could not be uploaded right now. Please try again.");
                await PopulateEventTypeOptionsAsync(model);
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
            var evnt = await (
                from item in _context.Events.AsNoTracking()
                join eventType in _context.EventTypes.AsNoTracking() on item.EventTypeId equals eventType.Id into eventTypeJoin
                from eventType in eventTypeJoin.DefaultIfEmpty()
                where item.Id == id
                select new
                {
                    Event = item,
                    EventTypeName = eventType != null ? eventType.Name : "Unknown type"
                })
                .FirstOrDefaultAsync();

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
                    VenueAvailability = venue != null ? venue.Availability : VenueAvailabilityCatalog.Unavailable,
                    BookingDate = booking.BookingDate,
                    Status = booking.Status
                })
                .Take(5)
                .ToListAsync();

            var model = new EventDetailsViewModel
            {
                Id = evnt.Event.Id,
                Name = evnt.Event.Name,
                Description = evnt.Event.Description,
                ImageUrl = ResolveEventImageUrl(evnt.Event.ImageUrl),
                EventTypeName = evnt.EventTypeName,
                StartDateTime = evnt.Event.StartDateTime,
                EndDateTime = evnt.Event.EndDateTime,
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

            var model = new EventFormViewModel
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Description = evnt.Description,
                EventTypeId = evnt.EventTypeId,
                ImageUrl = ResolveEventImageUrl(evnt.ImageUrl),
                StartDateTime = evnt.StartDateTime,
                EndDateTime = evnt.EndDateTime
            };

            await PopulateEventTypeOptionsAsync(model);
            return View("~/Views/Events/Edit.cshtml", model);
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
            await ValidateEventTypeAsync(model.EventTypeId);

            if (!ModelState.IsValid)
            {
                model.ImageUrl = ResolveEventImageUrl(model.ImageUrl);
                await PopulateEventTypeOptionsAsync(model);
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
            evnt.EventTypeId = model.EventTypeId;
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
                await PopulateEventTypeOptionsAsync(model);
                return View("~/Views/Events/Edit.cshtml", model);
            }
            catch (RequestFailedException)
            {
                ModelState.AddModelError(string.Empty, "The event image could not be uploaded right now. Please try again.");
                model.ImageUrl = ResolveEventImageUrl(previousImage);
                await PopulateEventTypeOptionsAsync(model);
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
            var evnt = await (
                from item in _context.Events.AsNoTracking()
                join eventType in _context.EventTypes.AsNoTracking() on item.EventTypeId equals eventType.Id into eventTypeJoin
                from eventType in eventTypeJoin.DefaultIfEmpty()
                where item.Id == id
                select new
                {
                    Event = item,
                    EventTypeName = eventType != null ? eventType.Name : "Unknown type"
                })
                .FirstOrDefaultAsync();

            if (evnt == null)
            {
                TempData["ErrorMessage"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Events/Delete.cshtml", new EventDetailsViewModel
            {
                Id = evnt.Event.Id,
                Name = evnt.Event.Name,
                Description = evnt.Event.Description,
                ImageUrl = ResolveEventImageUrl(evnt.Event.ImageUrl),
                EventTypeName = evnt.EventTypeName,
                StartDateTime = evnt.Event.StartDateTime,
                EndDateTime = evnt.Event.EndDateTime,
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

        private async Task ValidateEventTypeAsync(int eventTypeId)
        {
            if (!await _context.EventTypes.AnyAsync(eventType => eventType.Id == eventTypeId))
            {
                ModelState.AddModelError(nameof(EventFormViewModel.EventTypeId), "Please select a valid event type.");
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

        private async Task PopulateEventTypeOptionsAsync(EventFormViewModel model)
        {
            model.EventTypeOptions = await _context.EventTypes
                .AsNoTracking()
                .OrderBy(eventType => eventType.Name)
                .Select(eventType => new SelectListItem
                {
                    Value = eventType.Id.ToString(),
                    Text = eventType.Name
                })
                .ToListAsync();
        }

        private async Task PopulateEventFilterSelectionsAsync(EventDirectoryViewModel model)
        {
            model.EventTypeOptions = await _context.EventTypes
                .AsNoTracking()
                .OrderBy(eventType => eventType.Name)
                .Select(eventType => new SelectListItem
                {
                    Value = eventType.Id.ToString(),
                    Text = eventType.Name
                })
                .ToListAsync();

            model.VenueOptions = await _context.Venues
                .AsNoTracking()
                .OrderBy(venue => venue.Name)
                .Select(venue => new SelectListItem
                {
                    Value = venue.Id.ToString(),
                    Text = $"{venue.Name} - {venue.Location}"
                })
                .ToListAsync();

            model.VenueAvailabilityOptions = VenueAvailabilityCatalog.All
                .Select(availability => new SelectListItem
                {
                    Value = availability,
                    Text = availability
                })
                .ToList();
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
