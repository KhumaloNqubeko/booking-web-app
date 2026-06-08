# Local Development Setup

The repository intentionally contains no database passwords or Azure Storage keys.

## PostgreSQL development

Configure local secrets from the project directory:

```powershell
dotnet user-secrets set "DatabaseProvider" "PostgreSql"
dotnet user-secrets set "ConnectionStrings:PostgreSqlConnection" "Host=localhost;Port=5432;Database=booking;Username=<user>;Password=<password>;Timezone=UTC"
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<storage-connection-string>"
```

Then run:

```powershell
dotnet run
```

## Azure SQL development

```powershell
dotnet user-secrets set "DatabaseProvider" "SqlServer"
dotnet user-secrets set "ConnectionStrings:AzureSqlConnection" "<azure-sql-connection-string>"
```

Production secrets belong in Azure App Service environment variables, not `appsettings.json`.

Any PostgreSQL password or Azure Storage account key that was previously committed must be rotated in the relevant service.
