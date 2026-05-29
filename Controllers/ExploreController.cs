using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("Explore")]
    public class ExploreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExploreController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? query,
            string scope = "All",
            string? bookingStatus = null,
            Guid? venueId = null,
            int? eventTypeId = null,
            string? venueAvailability = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "All" : scope;
            var term = query?.Trim();
            var loweredTerm = term?.ToLower();

            var model = new SearchResultsViewModel
            {
                Query = term,
                Scope = normalizedScope,
                BookingStatus = bookingStatus,
                VenueId = venueId,
                EventTypeId = eventTypeId,
                VenueAvailability = venueAvailability,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            await PopulateFilterSelectionsAsync(model);

            if (normalizedScope is "All" or "Venues")
            {
                var venueQuery = _context.Venues.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(loweredTerm))
                {
                    venueQuery = venueQuery.Where(v =>
                        v.Name.ToLower().Contains(loweredTerm) ||
                        v.Location.ToLower().Contains(loweredTerm));
                }

                if (venueId.HasValue)
                {
                    venueQuery = venueQuery.Where(v => v.Id == venueId.Value);
                }

                if (!string.IsNullOrWhiteSpace(venueAvailability))
                {
                    venueQuery = venueQuery.Where(v => v.Availability == venueAvailability);
                }

                var bookingCounts = await _context.Bookings
                    .AsNoTracking()
                    .GroupBy(b => b.VenueId)
                    .Select(group => new { group.Key, Count = group.Count() })
                    .ToDictionaryAsync(item => item.Key, item => item.Count);

                var venues = await venueQuery
                    .OrderBy(v => v.Name)
                    .Select(v => new VenueListItemViewModel
                    {
                        Id = v.Id,
                        Name = v.Name,
                        Location = v.Location,
                        Capacity = v.Capacity,
                        Availability = v.Availability,
                        ImageUrl = v.ImageUrl
                    })
                    .Take(12)
                    .ToListAsync();

                foreach (var venue in venues)
                {
                    venue.BookingCount = bookingCounts.GetValueOrDefault(venue.Id);
                    venue.ImageUrl = ResolveVenueImageUrl(venue.ImageUrl);
                }

                model.Venues = venues;
            }

            if (normalizedScope is "All" or "Events")
            {
                var eventQuery =
                    from evnt in _context.Events.AsNoTracking()
                    join eventType in _context.EventTypes.AsNoTracking() on evnt.EventTypeId equals eventType.Id
                    select new
                    {
                        Event = evnt,
                        EventTypeName = eventType.Name
                    };

                if (!string.IsNullOrWhiteSpace(loweredTerm))
                {
                    eventQuery = eventQuery.Where(e =>
                        e.Event.Name.ToLower().Contains(loweredTerm) ||
                        e.Event.Description.ToLower().Contains(loweredTerm) ||
                        e.EventTypeName.ToLower().Contains(loweredTerm));
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
                    .Take(12)
                    .ToListAsync();

                foreach (var evnt in events)
                {
                    evnt.BookingCount = bookingCounts.GetValueOrDefault(evnt.Id);
                    evnt.ImageUrl = ResolveEventImageUrl(evnt.ImageUrl);
                }

                model.Events = events;
            }

            if (normalizedScope is "All" or "Bookings")
            {
                var bookingQuery =
                    from booking in _context.Bookings.AsNoTracking()
                    join venue in _context.Venues.AsNoTracking() on booking.VenueId equals venue.Id
                    join evnt in _context.Events.AsNoTracking() on booking.EventId equals evnt.Id
                    join eventType in _context.EventTypes.AsNoTracking() on evnt.EventTypeId equals eventType.Id
                    select new BookingListItemViewModel
                    {
                        Id = booking.Id,
                        VenueId = booking.VenueId,
                        EventId = booking.EventId,
                        VenueName = venue.Name,
                        EventName = evnt.Name,
                        EventTypeName = eventType.Name,
                        VenueAvailability = venue.Availability,
                        BookingDate = booking.BookingDate,
                        Status = booking.Status
                    };

                if (!string.IsNullOrWhiteSpace(loweredTerm))
                {
                    bookingQuery = bookingQuery.Where(b =>
                        b.VenueName.ToLower().Contains(loweredTerm) ||
                        b.EventName.ToLower().Contains(loweredTerm) ||
                        b.EventTypeName.ToLower().Contains(loweredTerm) ||
                        b.Status.ToLower().Contains(loweredTerm));
                }

                if (!string.IsNullOrWhiteSpace(bookingStatus))
                {
                    bookingQuery = bookingQuery.Where(b => b.Status == bookingStatus);
                }

                if (venueId.HasValue)
                {
                    bookingQuery = bookingQuery.Where(b => b.VenueId == venueId.Value);
                }

                if (eventTypeId.HasValue)
                {
                    bookingQuery = bookingQuery.Where(b =>
                        _context.Events.Any(evnt => evnt.Id == b.EventId && evnt.EventTypeId == eventTypeId.Value));
                }

                if (!string.IsNullOrWhiteSpace(venueAvailability))
                {
                    bookingQuery = bookingQuery.Where(b => b.VenueAvailability == venueAvailability);
                }

                if (dateFrom.HasValue)
                {
                    bookingQuery = bookingQuery.Where(b => b.BookingDate.Date >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    bookingQuery = bookingQuery.Where(b => b.BookingDate.Date <= dateTo.Value.Date);
                }

                model.Bookings = await bookingQuery
                    .OrderByDescending(b => b.BookingDate)
                    .Take(20)
                    .ToListAsync();
            }

            return View("~/Views/Explore/Index.cshtml", model);
        }

        private async Task PopulateFilterSelectionsAsync(SearchResultsViewModel model)
        {
            model.VenueOptions = await _context.Venues
                .AsNoTracking()
                .OrderBy(venue => venue.Name)
                .Select(venue => new SelectListItem
                {
                    Value = venue.Id.ToString(),
                    Text = $"{venue.Name} - {venue.Location}"
                })
                .ToListAsync();

            model.EventTypeOptions = await _context.EventTypes
                .AsNoTracking()
                .OrderBy(eventType => eventType.Name)
                .Select(eventType => new SelectListItem
                {
                    Value = eventType.Id.ToString(),
                    Text = eventType.Name
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
    }
}
