namespace Booking_webapp.Models.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int VenueCount { get; set; }
        public int EventCount { get; set; }
        public int BookingCount { get; set; }
        public List<DashboardBookingViewModel> RecentBookings { get; set; } = new();
    }

    public class DashboardBookingViewModel
    {
        public Guid Id { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
