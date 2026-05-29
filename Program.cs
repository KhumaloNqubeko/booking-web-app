using Booking_webapp.Data;
using Booking_webapp.Models.Options;
using Booking_webapp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<AzureBlobStorageOptions>(
    builder.Configuration.GetSection(AzureBlobStorageOptions.SectionName));
builder.Services.AddSingleton<IBlobImageStorageService, BlobImageStorageService>();

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSql";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var sqlServerConnection =
            builder.Configuration.GetConnectionString("AzureSqlConnection") ??
            builder.Configuration.GetConnectionString("DefaultConnection");

        options.UseSqlServer(sqlServerConnection);
        return;
    }

    var postgresConnection =
        builder.Configuration.GetConnectionString("PostgreSqlConnection") ??
        builder.Configuration.GetConnectionString("DefaultConnection");

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
