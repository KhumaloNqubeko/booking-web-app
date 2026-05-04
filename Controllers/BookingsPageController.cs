using Booking_webapp.Data;
using Booking_webapp.Models.Entities;
using Booking_webapp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("Bookings")]
    public class BookingsPageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsPageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            string? status = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                bookingQuery = bookingQuery.Where(b =>
                    EF.Functions.ILike(b.VenueName, $"%{term}%") ||
                    EF.Functions.ILike(b.EventName, $"%{term}%") ||
                    EF.Functions.ILike(b.Status, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                bookingQuery = bookingQuery.Where(b => b.Status == status);
            }

            if (dateFrom.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.BookingDate.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.BookingDate.Date <= dateTo.Value.Date);
            }

            var bookings = await bookingQuery
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var model = new BookingBoardViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalCount = bookings.Count,
                ConfirmedCount = bookings.Count(b => b.Status == "Confirmed"),
                PendingCount = bookings.Count(b => b.Status == "Pending"),
                CancelledCount = bookings.Count(b => b.Status == "Cancelled"),
                Bookings = bookings
            };

            return View("~/Views/Bookings/Index.cshtml", model);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new BookingFormViewModel();
            await PopulateBookingSelectionsAsync(model);
            return View("~/Views/Bookings/Create.cshtml", model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingFormViewModel model)
        {
            await ValidateBookingAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateBookingSelectionsAsync(model);
                return View("~/Views/Bookings/Create.cshtml", model);
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                VenueId = model.VenueId,
                EventId = model.EventId,
                BookingDate = model.BookingDate,
                Status = model.Status
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var model = await BuildBookingDetailsAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "The selected booking could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Bookings/Details.cshtml", model);
        }

        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "The selected booking could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new BookingFormViewModel
            {
                Id = booking.Id,
                VenueId = booking.VenueId,
                EventId = booking.EventId,
                BookingDate = booking.BookingDate,
                Status = booking.Status
            };

            await PopulateBookingSelectionsAsync(model);
            return View("~/Views/Bookings/Edit.cshtml", model);
        }

        [HttpPost("Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BookingFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "The selected booking could not be found.";
                return RedirectToAction(nameof(Index));
            }

            await ValidateBookingAsync(model, id);

            if (!ModelState.IsValid)
            {
                await PopulateBookingSelectionsAsync(model);
                return View("~/Views/Bookings/Edit.cshtml", model);
            }

            booking.VenueId = model.VenueId;
            booking.EventId = model.EventId;
            booking.BookingDate = model.BookingDate;
            booking.Status = model.Status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await BuildBookingDetailsAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "The selected booking could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Bookings/Delete.cshtml", model);
        }

        [HttpPost("Delete/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "The selected booking could not be found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<BookingDetailsViewModel?> BuildBookingDetailsAsync(Guid id)
        {
            return await (
                from booking in _context.Bookings.AsNoTracking()
                join venue in _context.Venues.AsNoTracking() on booking.VenueId equals venue.Id into venueJoin
                from venue in venueJoin.DefaultIfEmpty()
                join evnt in _context.Events.AsNoTracking() on booking.EventId equals evnt.Id into eventJoin
                from evnt in eventJoin.DefaultIfEmpty()
                where booking.Id == id
                select new BookingDetailsViewModel
                {
                    Id = booking.Id,
                    VenueId = booking.VenueId,
                    EventId = booking.EventId,
                    VenueName = venue != null ? venue.Name : "Unknown venue",
                    EventName = evnt != null ? evnt.Name : "Unknown event",
                    BookingDate = booking.BookingDate,
                    Status = booking.Status
                })
                .FirstOrDefaultAsync();
        }

        private async Task PopulateBookingSelectionsAsync(BookingFormViewModel model)
        {
            model.VenueOptions = await _context.Venues
                .AsNoTracking()
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.Name} - {v.Location}"
                })
                .ToListAsync();

            model.EventOptions = await _context.Events
                .AsNoTracking()
                .OrderBy(e => e.StartDateTime)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Name} - {e.StartDateTime:dd MMM yyyy}"
                })
                .ToListAsync();

            model.StatusOptions = new List<SelectListItem>
            {
                new() { Value = "Pending", Text = "Pending" },
                new() { Value = "Confirmed", Text = "Confirmed" },
                new() { Value = "Cancelled", Text = "Cancelled" }
            };
        }

        private async Task ValidateBookingAsync(BookingFormViewModel model, Guid? bookingIdToExclude = null)
        {
            if (!await _context.Venues.AnyAsync(v => v.Id == model.VenueId))
            {
                ModelState.AddModelError(nameof(model.VenueId), "Please select a valid venue.");
            }

            if (!await _context.Events.AnyAsync(e => e.Id == model.EventId))
            {
                ModelState.AddModelError(nameof(model.EventId), "Please select a valid event.");
            }

            var conflictingBookingExists = await _context.Bookings.AnyAsync(b =>
                b.VenueId == model.VenueId &&
                b.Id != bookingIdToExclude &&
                b.BookingDate.Date == model.BookingDate.Date);

            if (conflictingBookingExists)
            {
                ModelState.AddModelError(string.Empty, "This venue is already booked for the selected date.");
            }
        }
    }
}
