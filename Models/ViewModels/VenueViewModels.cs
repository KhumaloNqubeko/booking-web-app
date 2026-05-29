using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Booking_webapp.Models.ViewModels
{
    public class VenueFormViewModel
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Venue name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int Capacity { get; set; }

        [Required]
        public string Availability { get; set; } = VenueAvailabilityCatalog.Available;

        [Display(Name = "Current image")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Upload image")]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> AvailabilityOptions { get; set; } = new();
    }

    public class VenueListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Availability { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int BookingCount { get; set; }
    }

    public class VenueDirectoryViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Availability { get; set; }
        public List<SelectListItem> AvailabilityOptions { get; set; } = new();
        public List<VenueListItemViewModel> Venues { get; set; } = new();
    }

    public class VenueDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Availability { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int BookingCount { get; set; }
        public List<RelatedBookingViewModel> RelatedBookings { get; set; } = new();
    }

    public class RelatedBookingViewModel
    {
        public Guid Id { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventTypeName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
