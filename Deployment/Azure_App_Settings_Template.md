# Azure App Service Configuration

Configure these values in **App Service > Environment variables**. Do not place real credentials in source control.

| Name | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DatabaseProvider` | `SqlServer` |
| `ConnectionStrings__AzureSqlConnection` | Azure SQL connection string |
| `AzureBlobStorage__ConnectionString` | Azure Storage connection string |
| `AzureBlobStorage__VenueContainerName` | `eventease-venues` |
| `AzureBlobStorage__EventContainerName` | `eventease-events` |

Expected public URL:

`https://STxxx.azurewebsites.net`

After publishing:

1. Open the public URL and confirm the dashboard loads.
2. Open `/Events`, `/Bookings`, and `/Explore`.
3. Test an empty filter submission.
4. Test Event Type + date range + venue availability together.
5. Run `AzureSql_Verification_Queries.sql` in Azure SQL Query Editor.
6. Capture the URL, Query Editor results, and deployed filter results.
