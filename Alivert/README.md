# Alivert (MVP sample)

This is a simplified "TradrPro-style" sample project: a marketing landing page, authentication, and a user area where you can create custom asset alerts.

## What's included

- ASP.NET Core Razor Pages app
- Local auth (Identity)
- Alert CRUD (Create / Read / Update / Delete)
- Background worker that evaluates alerts once per minute
- Configurable market data provider: `Fake` by default, optional Binance public ticker for crypto symbols such as `BTCUSDT`
- RSI/EMA alert options inspired by the RSI service: RSI threshold, RSI oversold + EMA cross up, RSI overbought + EMA cross down, and price zones
- Notification settings for Webhook, Discord, and Telegram
- Delivery logs for sent, failed, and skipped notification attempts
- Simple credit-based limits model (Free/Plus) and an "unlimited until" Pro flag

## Pages

- `/` Landing
- `/Pricing` Pricing
- `/HowItWorks` How it works
- `/App/Dashboard` User dashboard (auth)
- `/App/Alerts` Alerts CRUD (auth)
- `/App/Settings` Notification destination settings (auth)

## Market data

By default the app uses the local fake market data provider so development works without external dependencies.
To use Binance public ticker data for crypto pairs, set:

```json
"MarketData": {
  "Provider": "Binance",
  "BinanceBaseUrl": "https://api.binance.com",
  "RequestTimeoutSeconds": 8,
  "FallbackToFake": true
}
```

If Binance cannot resolve a symbol and `FallbackToFake` is `true`, the app falls back to generated demo data.

## Notifications

Users configure destinations in `/App/Settings`.

- `Webhook`: posts a JSON payload to the configured URL.
- `Discord`: posts alert text to a Discord webhook URL.
- `Telegram`: requires `Notifications:TelegramBotToken` in app settings and a user-level chat ID.
- `Email`: currently tracked as skipped until an SMTP/email transport is connected.

## Alert rule options

- `PriceAbove` / `PriceBelow`: compare the latest price to the threshold.
- `PercentDrop24h` / `PercentRise24h`: compare 24h percentage change to the threshold.
- `VolumeAbove24h`: compare 24h quote volume to the threshold.
- `PriceZone`: triggers when price enters `threshold +/- ZonePercent`.
- `RsiBelow` / `RsiAbove`: compare RSI to the threshold on the selected timeframe.
- `RsiOversoldEmaCrossUp`: arms when RSI is at or below the threshold, then triggers when fast EMA crosses above slow EMA.
- `RsiOverboughtEmaCrossDown`: arms when RSI is at or above the threshold, then triggers when fast EMA crosses below slow EMA.

## Credits model (summary)

- **Free/Plus**: credits are consumed by alert activity/triggers, depending on your business rules.
- **Credit packs**: 25 credits for EUR 25, 50 credits for EUR 35, 100 credits for EUR 50.
- **Unlimited**: EUR 50/month or EUR 300/year until `UnlimitedUntilUtc` (paid-through date), no credit consumption.

## Run locally

1. Configure the connection string in `appsettings.json`
2. Apply migrations / create the database
3. Run the project:
   - `dotnet run --project Alivert`
