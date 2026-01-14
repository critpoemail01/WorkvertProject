# Alivert (MVP sample)

This is a simplified “TradrPro-style” sample project: a marketing landing page + authentication + a user area where you can create custom asset alerts.

## What’s included

- ASP.NET Core Razor Pages app
- Local auth (Identity)
- Alert CRUD (Create / Read / Update / Delete)
- Background worker that evaluates alerts once per minute (with a fake market data provider)
- Simple credit-based limits model (Free/Plus) and an “unlimited until” Pro flag

## Pages

- `/` Landing
- `/Pricing` Pricing
- `/HowItWorks` How it works
- `/App/Dashboard` User dashboard (auth)
- `/App/Alerts` Alerts CRUD (auth)

## Credits model (summary)

- **Free/Plus**: credits are consumed by alert activity/triggers, depending on your business rules.
- **Pro**: unlimited until `UnlimitedUntilUtc` (paid-through date), no credit consumption.

> Note: This repository is an MVP/demo. Replace the fake market data provider with your real provider.

## Run locally

1. Configure the connection string in `appsettings.json`
2. Apply migrations / create the database
3. Run the project:
   - `dotnet run --project Alivert`

