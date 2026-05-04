using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Booking_webapp.Models.ViewModels
{
    public class EventFormViewModel
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Event name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Current image")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Upload image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Start time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartDateTime { get; set; } = DateTime.Now;

        [Display(Name = "End time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime EndDateTime { get; set; } = DateTime.Now.AddHours(2);
    }

    public class EventListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int BookingCount { get; set; }
    }

    public class EventDirectoryViewModel
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartFrom { get; set; }
        public List<EventListItemViewModel> Events { get; set; } = new();
    }

    public class EventDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int BookingCount { get; set; }
        public List<EventBookingViewModel> RelatedBookings { get; set; } = new();
    }

    public class EventBookingViewModel
    {
        public Guid Id { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
