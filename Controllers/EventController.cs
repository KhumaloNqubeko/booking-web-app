using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        public readonly ApplicationDbContext dbContext;

        public EventController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetEvent(Guid id)
        {
            var evnt = await dbContext.Events.FindAsync(id);

            if (evnt == null)
            {
                return NotFound(new { message = "The requested event was not found." });
            }

            return Ok(evnt);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvents(
            int? eventTypeId = null,
            Guid? venueId = null,
            string? venueAvailability = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var eventQuery = dbContext.Events.AsQueryable();

            if (eventTypeId.HasValue)
            {
                eventQuery = eventQuery.Where(evnt => evnt.EventTypeId == eventTypeId.Value);
            }

            if (dateFrom.HasValue)
            {
                eventQuery = eventQuery.Where(evnt => evnt.StartDateTime.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                eventQuery = eventQuery.Where(evnt => evnt.EndDateTime.Date <= dateTo.Value.Date);
            }

            if (venueId.HasValue)
            {
                eventQuery = eventQuery.Where(evnt =>
                    dbContext.Bookings.Any(booking => booking.EventId == evnt.Id && booking.VenueId == venueId.Value));
            }

            if (!string.IsNullOrWhiteSpace(venueAvailability))
            {
                eventQuery = eventQuery.Where(evnt =>
                    (from booking in dbContext.Bookings
                     join venue in dbContext.Venues on booking.VenueId equals venue.Id
                     where booking.EventId == evnt.Id
                     select venue.Availability)
                    .Any(availability => availability == venueAvailability));
            }

            var events = await eventQuery.ToListAsync();
            return Ok(events);
        }

        [HttpPost]
        public async Task<IActionResult> PostEvent(EventDto eventDto)
        {
            await ValidateEventTypeAsync(eventDto.EventTypeId);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var evnt = new Event
            {
                Name = eventDto.Name,
                Description = eventDto.Description,
                EventTypeId = eventDto.EventTypeId,
                StartDateTime = eventDto.StartDateTime,
                EndDateTime = eventDto.EndDateTime
            };

            dbContext.Events.Add(evnt);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = evnt.Id }, evnt);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateEvent(Guid id, EventDto eventDto)
        {
            var evnt = await dbContext.Events.FindAsync(id);

            if (evnt == null)
            {
                return NotFound(new { message = "The requested event was not found." });
            }

            await ValidateEventTypeAsync(eventDto.EventTypeId);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            evnt.Name = eventDto.Name;
            evnt.Description = eventDto.Description;
            evnt.EventTypeId = eventDto.EventTypeId;
            evnt.StartDateTime = eventDto.StartDateTime;
            evnt.EndDateTime = eventDto.EndDateTime;

            await dbContext.SaveChangesAsync();

            return Ok(evnt);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var evnt = await dbContext.Events.FindAsync(id);

            if (evnt == null)
            {
                return NotFound(new { message = "The requested event was not found." });
            }

            var hasBookings = await dbContext.Bookings.AnyAsync(b => b.EventId == id);

            if (hasBookings)
            {
                return Conflict(new { message = "This event cannot be deleted because it is linked to existing bookings." });
            }

            dbContext.Events.Remove(evnt);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }

        private async Task ValidateEventTypeAsync(int eventTypeId)
        {
            if (!await dbContext.EventTypes.AnyAsync(eventType => eventType.Id == eventTypeId))
            {
                ModelState.AddModelError(nameof(EventDto.EventTypeId), "Please select a valid event type.");
            }
        }
    }
}
