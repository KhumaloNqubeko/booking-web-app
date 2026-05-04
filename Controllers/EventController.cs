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
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await dbContext.Events.ToListAsync();
            return Ok(events);
        }

        [HttpPost]
        public async Task<IActionResult> PostEvent(EventDto eventDto)
        {
            var evnt = new Event
            {
                Name = eventDto.Name,
                Description = eventDto.Description,
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

            evnt.Name = eventDto.Name;
            evnt.Description = eventDto.Description;
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

            // Block deletes for events that already have linked bookings.
            var hasBookings = await dbContext.Bookings.AnyAsync(b => b.EventId == id);

            if (hasBookings)
            {
                return Conflict(new { message = "This event cannot be deleted because it is linked to existing bookings." });
            }

            dbContext.Events.Remove(evnt);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }
    }
}
