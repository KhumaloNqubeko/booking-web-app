# Part 3 Azure Deployment Notes

## 1. Target Deployment Summary

- Web App URL: `https://<your-app-service-name>.azurewebsites.net`
- Database provider for Azure deployment: `SqlServer`
- Azure SQL connection setting name: `ConnectionStrings__AzureSqlConnection`
- Blob storage setting section: `AzureBlobStorage`

## 2. Required Azure App Settings

Set the following values in Azure App Service configuration:

- `DatabaseProvider=SqlServer`
- `ConnectionStrings__AzureSqlConnection=Server=tcp:<your-server>.database.windows.net,1433;Initial Catalog=<your-database>;Persist Security Info=False;User ID=<your-user>;Password=<your-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
- `AzureBlobStorage__ConnectionString=<your-blob-connection-string>`
- `AzureBlobStorage__VenueContainerName=eventease-venues`
- `AzureBlobStorage__EventContainerName=eventease-events`

## 3. Database Update Evidence Required

Capture the following after deployment:

- Azure SQL Query Editor screenshot showing the `EventTypes` table populated with the predefined categories
- Azure SQL Query Editor screenshot showing the `Venues` table with the `Availability` column
- Azure SQL Query Editor screenshot or result grid showing `Events.EventTypeId`
- Azure SQL Query Editor screenshot or result grid showing the booking foreign key relationships in use

## 4. Blob Storage Evidence Required

If uploaded venue or event images are used in the deployed version, capture:

- Blob container screenshot for `eventease-venues`
- Blob container screenshot for `eventease-events`
- Running web app screenshot showing an uploaded image rendered from storage

## 5. Migration Coverage Included In This Codebase

The project includes migration support for:

- `EventTypes` lookup table creation
- Seeded event type categories
- `Venues.Availability` column
- `Events.EventTypeId` foreign key field
- Booking foreign keys and related indexes
- Provider-aware migration behavior for PostgreSQL and SQL Server oriented deployment paths

## 6. Recommended Deployment Validation Steps

1. Publish the latest build to Azure App Service.
2. Confirm the app starts successfully.
3. Open the deployed URL and test:
   - event type filtering
   - venue filtering
   - venue availability filtering
   - date range filtering
4. Open Azure SQL Query Editor and confirm the new schema is present.
5. Upload or display an existing event or venue image and confirm blob-backed media renders in the deployed app.

## 7. Submission Reminder

Before submitting, replace placeholders with real values and add screenshots for each evidence item listed above.
