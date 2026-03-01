# FlightTracker

Stateless .NET 10 Worker that runs once and exits: fetches the lowest flight price from Amadeus, and sends a Telegram notification. Ideal for running as a Cron Job (e.g. Render Cron Jobs or GitHub Actions).

## Project structure

```
flight-tracker/
├── .github/
│   └── workflows/
│       └── flight-bot.yml          # GitHub Actions workflow (scheduled runs)
├── src/
│   ├── Common/
│   │   └── Constants.cs            # API endpoints, retry config
│   ├── Configuration/
│   │   ├── AmadeusOptions.cs       # Amadeus API credentials
│   │   ├── FlightOptions.cs        # Flight search parameters
│   │   └── TelegramOptions.cs      # Telegram bot config
│   ├── Services/
│   │   ├── Amadeus/
│   │   │   ├── IAmadeusFlightSearchService.cs
│   │   │   └── AmadeusFlightSearchService.cs    # OAuth2 + Flight Offers Search
│   │   ├── GoogleFlights/
│   │   │   └── GoogleFlightsLinkBuilder.cs      # Generate Google Flights URLs
│   │   └── Telegram/
│   │       ├── ITelegramNotificationService.cs
│   │       └── TelegramNotificationService.cs   # Send price alerts via Telegram
│   ├── Workers/
│   │   └── PriceMonitorWorker.cs   # BackgroundService (runs once then exits)
│   ├── .env                         # Local environment variables (gitignored)
│   ├── appsettings.json            # Configuration template
│   ├── FlightTracker.csproj
│   └── Program.cs                   # App entry point, DI setup
└── README.md
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Amadeus for Developers](https://developers.amadeus.com/) account (Test environment: API Key & Secret)
- Telegram bot (token from [@BotFather](https://t.me/botfather), Chat ID from getUpdates)

## Configuration

Create a `.env` file in the `src/` directory. Environment variables use `Section__Key` format (double underscore):

```env
# Flight search parameters
FLIGHT__ORIGIN=GRU
FLIGHT__DESTINATION=SCL
FLIGHT__DEPARTUREDATE=2026-12-28
FLIGHT__RETURNDATE=2027-01-03
FLIGHT__TARGETPRICE=2000

# Amadeus API (Test environment)
AMADEUS__CLIENTID=your_client_id_here
AMADEUS__CLIENTSECRET=your_client_secret_here

# Telegram bot
TELEGRAM__BOTTOKEN=your_bot_token_here
TELEGRAM__CHATID=your_chat_id_here
```

**Important**: 
- Dates must be in `YYYY-MM-DD` format
- Use IATA airport codes (3 letters, e.g., GRU, JFK, LHR)
- Get Amadeus credentials from [Amadeus Self-Service](https://developers.amadeus.com/self-service)
- Get Telegram Chat ID by messaging your bot and calling `https://api.telegram.org/bot<TOKEN>/getUpdates`

## How to run

### Local execution
```bash
cd src
dotnet run
```

### GitHub Actions
The workflow runs automatically at:
- **08:00 AM Brazil time** (11:00 UTC)
- **10:00 PM Brazil time** (01:00 UTC)

Or trigger manually via **Actions** tab → **Flight Tracker** → **Run workflow**.

**Required GitHub Secrets** (Settings → Secrets and variables → Actions):
- `AMADEUS_CLIENT_ID`
- `AMADEUS_CLIENT_SECRET`
- `TELEGRAM_BOT_TOKEN`
- `TELEGRAM_CHAT_ID`
- `FLIGHT__ORIGIN`
- `FLIGHT__DESTINATION`
- `FLIGHT__DEPARTUREDATE`
- `FLIGHT__RETURNDATE`

## Tech stack

- .NET 10, Worker Service (single run then `StopApplication`)
- HttpClient, Options pattern, [dotenv.net](https://github.com/bolorundurowb/dotenv.net)
- Amadeus Flight Offers Search API (OAuth2 Client Credentials)
- Telegram Bot API
- No database; fully stateless
