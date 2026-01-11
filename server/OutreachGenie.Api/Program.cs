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

// Register SignalR notification service
builder.Services.AddScoped<OutreachGenie.Api.Services.IAgentNotificationService, OutreachGenie.Api.Services.AgentNotificationService>();

builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS support for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "https://localhost:8080", "http://localhost:8081", "https://localhost:8081")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

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

// Enable CORS
app.UseCors("AllowReactFrontend");

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<OutreachGenie.Api.Hubs.AgentHub>("/hubs/agent");

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
