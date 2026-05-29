# Part 3 Reflective Technical Report

## 1. System Feature List

- Venue management with create, edit, view, delete, image upload, capacity, location, and availability status.
- Event management with create, edit, view, delete, image upload, event type classification, and date scheduling.
- Booking management with create, edit, view, delete, booking status tracking, and validation against venue availability.
- Cross-system search and filtering for venues, events, bookings, and the combined explore page.
- Event type lookup data with predefined categories:
  - Conference
  - Wedding
  - Concert
  - Seminar
  - Workshop
  - Birthday
  - Corporate Event
  - Exhibition
- Azure Blob Storage support for venue and event images.
- Database-first consistency through Entity Framework Core migrations and automatic startup migration application.
- MVC pages for user workflows and REST API endpoints for programmatic access.

## 2. How Each Feature Works

### Venue management
Venues are stored in the `Venues` table and managed through MVC pages and API endpoints. Each venue includes a name, location, capacity, image, and an `Availability` field. The availability field is now used in both filtering and booking validation.

### Event management
Events are stored in the `Events` table. Each event records its name, description, image, schedule, and `EventTypeId`. The event type foreign key links the event to a predefined record in the new `EventTypes` lookup table.

### Booking management
Bookings connect venues and events using `VenueId` and `EventId`. Users can track booking dates and statuses such as Pending, Confirmed, and Cancelled. During create and edit operations, the system now prevents bookings against venues marked as unavailable.

### Advanced filtering
The filtering feature was expanded across the UI and controller logic. Users can now filter by:

- Event type
- Date range
- Venue availability
- Venue

These filters work on both the visible forms and the server-side query logic, so returned results actually change based on the selected values.

### Explore page
The Explore page acts as a combined search surface for venues, events, and bookings. It now uses the same Part 3 filter set so a user can move across scopes without losing the same search concepts.

### Image handling
Event and venue images are stored through the blob storage service abstraction. The application is already structured to use Azure Blob Storage by connection string and container names from configuration.

## 3. Component Discussion

### Azure App Service
Azure App Service is suitable for hosting the MVC web application because it handles deployment, environment configuration, HTTPS, scaling, and managed hosting for ASP.NET Core applications. It reduces the amount of server administration required compared with a self-managed virtual machine.

Alternative:
- Azure Container Apps or Azure Kubernetes Service could be used for container-based deployment, but they would introduce more operational complexity than this project needs.

### Azure SQL Database
Azure SQL Database is appropriate for the relational data model because the system uses structured entities, foreign keys, and migrations. It also fits well with Entity Framework Core and Azure App Service deployment workflows.

Alternative:
- PostgreSQL on Azure would also work, especially because the project started on PostgreSQL locally.
- Cosmos DB would not be the best fit because the system depends on relational joins and lookup-table relationships.

### Azure Blob Storage
Azure Blob Storage is used for media files such as venue and event images. This keeps large file storage outside the relational database and matches a cloud-native pattern for handling uploaded assets.

Alternative:
- Images could be stored on the web server file system, but that is weaker for scaling and less reliable across deployments.
- Azure File Storage could be used, but Blob Storage is a better fit for static media objects.

### MVC / Web App
ASP.NET Core MVC provides clear separation between controllers, views, and models. It fits the project well because the system has form-heavy workflows, server-rendered views, validation, and CRUD pages.

Alternative:
- Razor Pages could simplify some CRUD scenarios.
- A separate SPA frontend with React or Angular could provide richer client-side interaction, but would add frontend complexity and API coupling.

### Entity Framework Core
Entity Framework Core manages database access, migrations, and model-to-table mapping. It helped keep the code aligned with the domain model and made it practical to introduce the new lookup table, foreign keys, and filtering changes.

Alternative:
- Dapper could be used for lighter-weight query control, but more manual mapping and migration discipline would be needed.
- Plain ADO.NET would provide maximum control but significantly more boilerplate.

## 4. Reflection From Part 1 To Part 3

### Design decisions
Part 1 established the CRUD foundation for venues, events, and bookings. Part 2 improved the user experience with better visuals, image handling, and stronger workflows. Part 3 extended the data model to support classification and richer querying rather than only basic record management.

One important design decision was introducing `EventType` as a lookup table instead of a free-text field. This keeps event categories consistent, improves filtering reliability, and supports relational integrity through a foreign key.

Another key decision was storing venue availability directly on the venue record. This made it possible to use availability in both filtering and booking validation without introducing unnecessary extra tables.

### Development challenges
The biggest technical challenge in Part 3 was making the application more deployment-ready while the existing project still carried PostgreSQL-specific assumptions. Search logic using `EF.Functions.ILike` had to be replaced with provider-neutral filtering, and the migration chain had to be adjusted so Azure SQL support was realistic instead of theoretical.

Another challenge was keeping the Part 1 and Part 2 pages stable while extending view models, forms, filters, and controllers across multiple areas at once. The safest approach was to follow the existing MVC structure and make targeted changes rather than trying to redesign the whole application.

### Deployment lessons
This part highlighted that deployment readiness is not only about publishing a web app. Configuration, provider differences, migration quality, and storage integration all matter. Supporting Azure SQL required more than adding a connection string; it required reviewing provider-specific code and migration behavior.

It also reinforced the value of documenting deployment evidence clearly. Screenshots, database evidence, and configuration notes are part of a professional delivery process, not just extras.

### Cloud skills developed
Through Part 3, the following cloud-focused skills were strengthened:

- Preparing configuration for multiple environments
- Structuring a web app for Azure App Service deployment
- Understanding Azure SQL connection and migration requirements
- Using Azure Blob Storage settings for uploaded media
- Linking application design decisions with deployment and evidence requirements

## 5. Conclusion

Part 3 moved the EventEase Venue Booking System from a functional CRUD application toward a more complete deployment-oriented solution. The system now supports richer filtering, better relational structure, Azure-oriented configuration, and submission-ready documentation. The work also showed how cloud preparation, data modelling, and user-facing search features depend on each other in a full-stack web application.
