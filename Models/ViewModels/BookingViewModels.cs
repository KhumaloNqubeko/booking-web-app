using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Booking_webapp.Models.ViewModels
{
    public class BookingFormViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Venue")]
        [Required]
        public Guid VenueId { get; set; }

        [Display(Name = "Event")]
        [Required]
        public Guid EventId { get; set; }

        [Display(Name = "Booking date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime BookingDate { get; set; } = DateTime.Today;

        [Required]
        public string Status { get; set; } = "Pending";

        public List<SelectListItem> VenueOptions { get; set; } = new();
        public List<SelectListItem> EventOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }

    public class BookingListItemViewModel
    {
        public Guid Id { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BookingBoardViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int TotalCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int PendingCount { get; set; }
        public int CancelledCount { get; set; }
        public List<BookingListItemViewModel> Bookings { get; set; } = new();
    }

    public class BookingDetailsViewModel
    {
        public Guid Id { get; set; }
        public Guid VenueId { get; set; }
        public Guid EventId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SearchResultsViewModel
    {
        public string? Query { get; set; }
        public string Scope { get; set; } = "All";
        public string? BookingStatus { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<VenueListItemViewModel> Venues { get; set; } = new();
        public List<EventListItemViewModel> Events { get; set; } = new();
        public List<BookingListItemViewModel> Bookings { get; set; } = new();
    }
}
