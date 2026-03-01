# FlightTracker

Stateless .NET 10 Worker that runs once and exits: fetches the lowest flight price from Amadeus, compares it to a target price, and sends a Telegram notification only when the price is less than or equal to the target. Ideal for running as a Cron Job (e.g. Render Cron Jobs or GitHub Actions).

## Project structure

```
FlightTracker/
├── Configuration/          # Options (Flight, Amadeus, Telegram)
├── Services/               # Amadeus search, Telegram notification, Google Flights link
├── Workers/                # PriceMonitorWorker (runs once then exits)
├── appsettings.json
└── Program.cs
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Amadeus for Developers](https://developers.amadeus.com/) (Test environment: API Key & Secret)
- Telegram bot (token from @BotFather, Chat ID from getUpdates)

## Configuration

Use a `.env` file (copy from `.env.example`) or appsettings. Env vars use `Section__Key` (e.g. `FLIGHT__ORIGIN`, `AMADEUS__CLIENTID`).

## How to run

```bash
dotnet run
```

The app runs once: fetches price → compares to target → sends Telegram if price ≤ target → exits. Schedule it with a cron (e.g. daily) on Render, GitHub Actions, or any host.

## Flow

1. Load config (env / appsettings).
2. Fetch lowest price from Amadeus (one-way or round-trip).
3. If no price found → log and exit.
4. If price > target → log and exit.
5. If price ≤ target → send Telegram message with price and Google Flights link → exit.

## Tech stack

- .NET 10, Worker Service (single run then `StopApplication`)
- HttpClient, Options, dotenv.net
- No database; stateless
