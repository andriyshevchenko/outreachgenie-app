using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Infrastructure.Persistence;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "OutreachGenie")
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "OutreachGenie",
                "logs",
                "outreachgenie-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Add services to the container.
builder.Services.AddDbContext<OutreachGenieDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IArtifactRepository, ArtifactRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();

builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations and initialize database on startup (skip for InMemory in tests)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OutreachGenieDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
}

// Add Serilog request logging
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Partial class declaration to make Program accessible for integration testing.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Protected constructor to satisfy SonarAnalyzer S1118.
    /// </summary>
    protected Program()
    {
    }
}
