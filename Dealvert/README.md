# Promovert

Promovert is a Razor Pages marketing workspace for turning an application URL, company or business idea into measurable campaigns.

Core workflows:

- Create campaign assets for TikTok, Instagram, Facebook, LinkedIn, email, SMS and webhooks.
- Manage active campaign capacity through free credits, credit packs or Unlimited plans.
- Track users reached, users interacted and users converted in the dashboard.
- Configure delivery windows and marketing channel destinations.

Run locally from the repository root:

```powershell
dotnet run --project Alivert\Promovert.csproj
```

Email sender profiles can be configured under `Notifications:Email:SenderProfiles`. Use only verified sender addresses on authenticated domains, with SPF/DKIM/DMARC configured by the mail provider. When multiple profiles are configured, Promovert distributes campaign recipients across those profiles instead of sending every email from the same mailbox.
