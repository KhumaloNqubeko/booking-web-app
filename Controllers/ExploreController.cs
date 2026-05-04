using Booking_webapp.Data;
using Booking_webapp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
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
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "All" : scope;
            var term = query?.Trim();

            var model = new SearchResultsViewModel
            {
                Query = term,
                Scope = normalizedScope,
                BookingStatus = bookingStatus,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            if (normalizedScope is "All" or "Venues")
            {
                var venueQuery = _context.Venues.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(term))
                {
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
                    .Select(v => new VenueListItemViewModel
                    {
                        Id = v.Id,
                        Name = v.Name,
                        Location = v.Location,
                        Capacity = v.Capacity,
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
                var eventQuery = _context.Events.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    eventQuery = eventQuery.Where(e =>
                        EF.Functions.ILike(e.Name, $"%{term}%") ||
                        EF.Functions.ILike(e.Description, $"%{term}%"));
                }

                if (dateFrom.HasValue)
                {
                    eventQuery = eventQuery.Where(e => e.StartDateTime.Date >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    eventQuery = eventQuery.Where(e => e.StartDateTime.Date <= dateTo.Value.Date);
                }

                var bookingCounts = await _context.Bookings
                    .AsNoTracking()
                    .GroupBy(b => b.EventId)
                    .Select(group => new { group.Key, Count = group.Count() })
                    .ToDictionaryAsync(item => item.Key, item => item.Count);

                var events = await eventQuery
                    .OrderBy(e => e.StartDateTime)
                    .Select(e => new EventListItemViewModel
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Description = e.Description,
                        ImageUrl = e.ImageUrl,
                        StartDateTime = e.StartDateTime,
                        EndDateTime = e.EndDateTime
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
                    join venue in _context.Venues.AsNoTracking() on booking.VenueId equals venue.Id into venueJoin
                    from venue in venueJoin.DefaultIfEmpty()
                    join evnt in _context.Events.AsNoTracking() on booking.EventId equals evnt.Id into eventJoin
                    from evnt in eventJoin.DefaultIfEmpty()
                    select new BookingListItemViewModel
                    {
                        Id = booking.Id,
                        VenueName = venue != null ? venue.Name : "Unknown venue",
                        EventName = evnt != null ? evnt.Name : "Unknown event",
                        BookingDate = booking.BookingDate,
                        Status = booking.Status
                    };

                if (!string.IsNullOrWhiteSpace(term))
                {
                    bookingQuery = bookingQuery.Where(b =>
                        EF.Functions.ILike(b.VenueName, $"%{term}%") ||
                        EF.Functions.ILike(b.EventName, $"%{term}%") ||
                        EF.Functions.ILike(b.Status, $"%{term}%"));
                }

                if (!string.IsNullOrWhiteSpace(bookingStatus))
                {
                    bookingQuery = bookingQuery.Where(b => b.Status == bookingStatus);
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
