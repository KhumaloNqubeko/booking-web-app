using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Booking_webapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : Controller
    {
        public readonly ApplicationDbContext dbContext;
        public EventController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        [HttpGet("{id}")]
        public ActionResult GetEvent(int id)
        {
            var evnt = dbContext.Events.Find(id);

            if (evnt == null)
            {
                throw new KeyNotFoundException($"Event with ID {id} was not found.");
            }

            return Ok(evnt);
        }

        [HttpGet]
        public IActionResult GetAllEvents()
        {
            var events = dbContext.Events.ToList();

            return Ok(events);
        }

        [HttpPost]
        public IActionResult PostEvent(EventDto eventDto)
        {
        var evnt = new Event()
            {
                Name = eventDto.Name,
            Description = eventDto.Description,
            StartDateTime = eventDto.StartDateTime,
            EndDateTime = eventDto.EndDateTime
        };

            dbContext.Events.Add(evnt);
            dbContext.SaveChanges();

            return Ok(evnt);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateEvent(int id, EventDto eventDto)
        {
            var evnt = dbContext.Events.Find(id);

            if (evnt == null)
            {
                throw new KeyNotFoundException($"Event with ID {id} was not found.");
            }

            evnt.Name = eventDto.Name;
            evnt.Description = eventDto.Description;
            evnt.StartDateTime = eventDto.StartDateTime;
            evnt.EndDateTime = eventDto.EndDateTime;

            dbContext.SaveChanges();

            return Ok(evnt);
        }
    }
}
