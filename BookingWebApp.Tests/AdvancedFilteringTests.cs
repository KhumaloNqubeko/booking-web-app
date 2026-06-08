using Booking_webapp.Controllers;
using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Booking_webapp.Models.ViewModels;
using Booking_webapp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingWebApp.Tests;

public class AdvancedFilteringTests
{
    [Fact]
    public async Task EmptyEventFilters_ReturnAllEvents()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var controller = new EventsPageController(context, new FakeBlobStorageService());

        var result = await controller.Index();

        var model = Assert.IsType<EventDirectoryViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, model.Events.Count);
        Assert.True(controller.ModelState.IsValid);
        Assert.Contains(model.Events, item => item.Id == data.ConferenceEventId);
        Assert.Contains(model.Events, item => item.Id == data.WeddingEventId);
    }

    [Fact]
    public async Task CombinedEventFilters_ReturnOnlyMatchingEvent()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var controller = new EventsPageController(context, new FakeBlobStorageService());

        var result = await controller.Index(
            eventTypeId: EventTypeCatalog.Conference,
            venueId: data.AvailableVenueId,
            venueAvailability: VenueAvailabilityCatalog.Available,
            dateFrom: new DateTime(2026, 6, 5),
            dateTo: new DateTime(2026, 6, 6));

        var model = Assert.IsType<EventDirectoryViewModel>(Assert.IsType<ViewResult>(result).Model);
        var match = Assert.Single(model.Events);
        Assert.Equal(data.ConferenceEventId, match.Id);
        Assert.True(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task EventDateRange_IncludesEventsThatOverlapTheRange()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var controller = new EventsPageController(context, new FakeBlobStorageService());

        var result = await controller.Index(
            dateFrom: new DateTime(2026, 6, 5),
            dateTo: new DateTime(2026, 6, 6));

        var model = Assert.IsType<EventDirectoryViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Contains(model.Events, item => item.Id == data.ConferenceEventId);
    }

    [Fact]
    public async Task ReversedDateRange_AddsValidationError()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var controller = new EventsPageController(context, new FakeBlobStorageService());

        await controller.Index(
            dateFrom: new DateTime(2026, 6, 10),
            dateTo: new DateTime(2026, 6, 1));

        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState[nameof(EventDirectoryViewModel.DateTo)]!.Errors,
            error => error.ErrorMessage.Contains("end date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CombinedBookingFilters_ReturnOnlyMatchingBooking()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var controller = new BookingsPageController(context);

        var result = await controller.Index(
            status: BookingStatusCatalog.Confirmed,
            venueId: data.AvailableVenueId,
            eventTypeId: EventTypeCatalog.Conference,
            venueAvailability: VenueAvailabilityCatalog.Available,
            dateFrom: new DateTime(2026, 6, 5),
            dateTo: new DateTime(2026, 6, 5));

        var model = Assert.IsType<BookingBoardViewModel>(Assert.IsType<ViewResult>(result).Model);
        var match = Assert.Single(model.Bookings);
        Assert.Equal(data.ConferenceEventId, match.EventId);
        Assert.True(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task InvalidAvailability_AddsValidationError()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var controller = new BookingsPageController(context);

        await controller.Index(venueAvailability: "Maybe");

        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState["venueAvailability"]!.Errors,
            error => error.ErrorMessage.Contains("availability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnknownEventType_AddsValidationError()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var controller = new EventsPageController(context, new FakeBlobStorageService());

        await controller.Index(eventTypeId: 999);

        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState["eventTypeId"]!.Errors,
            error => error.ErrorMessage.Contains("event type", StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<TestData> SeedAsync(ApplicationDbContext context)
    {
        var availableVenue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Available Hall",
            Location = "Johannesburg",
            Capacity = 300,
            Availability = VenueAvailabilityCatalog.Available
        };
        var unavailableVenue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Closed Hall",
            Location = "Pretoria",
            Capacity = 200,
            Availability = VenueAvailabilityCatalog.Unavailable
        };
        var conference = new Booking_webapp.Models.Entities.Event
        {
            Id = Guid.NewGuid(),
            Name = "Cloud Conference",
            Description = "Cloud development",
            EventTypeId = EventTypeCatalog.Conference,
            StartDateTime = new DateTime(2026, 6, 1, 9, 0, 0),
            EndDateTime = new DateTime(2026, 6, 10, 17, 0, 0)
        };
        var wedding = new Booking_webapp.Models.Entities.Event
        {
            Id = Guid.NewGuid(),
            Name = "Winter Wedding",
            Description = "Wedding programme",
            EventTypeId = EventTypeCatalog.Wedding,
            StartDateTime = new DateTime(2026, 7, 1, 10, 0, 0),
            EndDateTime = new DateTime(2026, 7, 1, 18, 0, 0)
        };

        context.AddRange(availableVenue, unavailableVenue, conference, wedding);
        context.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(),
                VenueId = availableVenue.Id,
                EventId = conference.Id,
                BookingDate = new DateTime(2026, 6, 5),
                Status = BookingStatusCatalog.Confirmed
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                VenueId = unavailableVenue.Id,
                EventId = wedding.Id,
                BookingDate = new DateTime(2026, 7, 1),
                Status = BookingStatusCatalog.Pending
            });

        await context.SaveChangesAsync();

        return new TestData(
            availableVenue.Id,
            conference.Id,
            wedding.Id);
    }

    private sealed record TestData(
        Guid AvailableVenueId,
        Guid ConferenceEventId,
        Guid WeddingEventId);

    private sealed class FakeBlobStorageService : IBlobImageStorageService
    {
        public Task<string> UploadVenueImageAsync(IFormFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> UploadEventImageAsync(IFormFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task DeleteVenueImageAsync(string? storedReference, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteEventImageAsync(string? storedReference, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StoredImageFile?> OpenVenueImageAsync(
            string storedReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredImageFile?>(null);

        public Task<StoredImageFile?> OpenEventImageAsync(
            string storedReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredImageFile?>(null);
    }
}
