using dotenv.net;
using FlightTracker.Configuration;
using FlightTracker.Services;
using FlightTracker.Workers;

DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true, probeForEnv: true, probeLevelsToSearch: 4));

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<FlightOptions>(builder.Configuration.GetSection(FlightOptions.SectionName));
builder.Services.Configure<AmadeusOptions>(builder.Configuration.GetSection(AmadeusOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IFlightSearchService, AmadeusFlightSearchService>();
builder.Services.AddScoped<INotificationService, TelegramNotificationService>();
builder.Services.AddHostedService<PriceMonitorWorker>();

var host = builder.Build();
await host.RunAsync();
