# FlightTracker

.NET 10 Worker Service that checks flight prices for a given route daily, stores history in SQLite, and sends a Telegram notification with the current price (and whether the target price was reached).

## Project structure

```
FlightTracker/
├── Configuration/          # Options (Flight, Amadeus, Telegram)
├── Data/                   # DbContext, repositories, migrations
├── Domain/                 # FlightPrice entity
├── Services/               # Amadeus search, Telegram notification, Google Flights link
├── Workers/                # PriceMonitorWorker (BackgroundService)
├── Migrations/
├── appsettings.json
└── Program.cs
```
