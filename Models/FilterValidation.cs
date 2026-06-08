namespace Booking_webapp.Models
{
    public static class FilterValidation
    {
        public static bool IsDateRangeValid(DateTime? dateFrom, DateTime? dateTo)
        {
            return !dateFrom.HasValue ||
                   !dateTo.HasValue ||
                   dateFrom.Value.Date <= dateTo.Value.Date;
        }

        public static bool IsAvailabilityValid(string? availability)
        {
            return string.IsNullOrWhiteSpace(availability) ||
                   VenueAvailabilityCatalog.All.Contains(availability);
        }

        public static bool IsBookingStatusValid(string? status)
        {
            return string.IsNullOrWhiteSpace(status) ||
                   BookingStatusCatalog.All.Contains(status);
        }

        public static bool IsScopeValid(string? scope)
        {
            return string.IsNullOrWhiteSpace(scope) ||
                   SearchScopeCatalog.AllValues.Contains(scope);
        }
    }
}
