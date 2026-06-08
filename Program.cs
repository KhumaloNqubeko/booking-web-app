using Booking_webapp.Data;
using Booking_webapp.Models.Options;
using Booking_webapp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<AzureBlobStorageOptions>(options =>
{
    builder.Configuration
        .GetSection(AzureBlobStorageOptions.SectionName)
        .Bind(options);

    options.StorageConnection =
        builder.Configuration.GetConnectionString("AzureBlobStorage")
        ?? options.StorageConnection;
});
builder.Services.AddSingleton<IBlobImageStorageService, BlobImageStorageService>();

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSql";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var sqlServerConnection =
            builder.Configuration.GetConnectionString("AzureSqlConnection") ??
            builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(sqlServerConnection))
        {
            throw new InvalidOperationException(
                "SQL Server is selected but no connection string is configured. Set ConnectionStrings__AzureSqlConnection.");
        }

        options
            .UseSqlServer(sqlServerConnection)
            // This project keeps one provider-aware migration chain for local PostgreSQL
            // and Azure SQL. The checked-in snapshot carries PostgreSQL annotations.
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        return;
    }

    if (!databaseProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Unsupported database provider '{databaseProvider}'. Use 'PostgreSql' or 'SqlServer'.");
    }

    var postgresConnection =
        builder.Configuration.GetConnectionString("PostgreSqlConnection") ??
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(postgresConnection))
    {
        throw new InvalidOperationException(
            "PostgreSQL is selected but no connection string is configured. Set ConnectionStrings__PostgreSqlConnection.");
    }

    options.UseNpgsql(postgresConnection);
});

builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
