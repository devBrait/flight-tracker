using dotenv.net;
using FlightTracker.Configuration;
using FlightTracker.Data;
using FlightTracker.Services;
using FlightTracker.Workers;
using Microsoft.EntityFrameworkCore;

// Load .env from project root (if present). Env vars override appsettings.
// .NET convention: Section__Key → FLIGHT__ORIGIN, AMADEUS__CLIENTID, etc.
DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true, probeForEnv: true, probeLevelsToSearch: 4));

var builder = Host.CreateApplicationBuilder(args);

// --- Options configuration (bind from appsettings sections) ---
builder.Services.Configure<FlightOptions>(builder.Configuration.GetSection(FlightOptions.SectionName));
builder.Services.Configure<AmadeusOptions>(builder.Configuration.GetSection(AmadeusOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

// --- SQLite + EF Core ---
var dbPath = Path.Combine(builder.Environment.ContentRootPath ?? ".", "flight_tracker.db");
builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

// --- HttpClientFactory (for Amadeus and Telegram) ---
builder.Services.AddHttpClient();

// --- Repositories ---
builder.Services.AddScoped<IFlightPriceRepository, FlightPriceRepository>();

// --- Services ---
builder.Services.AddScoped<IFlightSearchService, AmadeusFlightSearchService>();
builder.Services.AddScoped<INotificationService, TelegramNotificationService>();

// --- Worker (HostedService) ---
builder.Services.AddHostedService<PriceMonitorWorker>();

var host = builder.Build();

// --- Ensure database exists and is migrated on startup ---
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightTrackerDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
