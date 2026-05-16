using System.Security.Cryptography;
using System.Text;

namespace Dealvert.Services;

public static class EmailSenderPool
{
    public static IReadOnlyList<EmailSenderProfile> GetConfiguredSenders(EmailNotificationOptions options)
    {
        var senders = options.SenderProfiles
            .Select(profile => FromProfile(profile, options))
            .Where(profile => profile is not null)
            .Cast<EmailSenderProfile>()
            .ToList();

        if (senders.Count > 0)
            return senders;

        return LegacyProfileConfigured(options)
            ?
            [
                new EmailSenderProfile(
                    "Primary",
                    string.IsNullOrWhiteSpace(options.FromName) ? "Dealvert" : options.FromName.Trim(),
                    options.FromEmail!.Trim(),
                    string.IsNullOrWhiteSpace(options.Username) ? options.FromEmail!.Trim() : options.Username.Trim(),
                    options.Password!,
                    options.SmtpServer.Trim(),
                    options.SmtpPort,
                    options.EnableSsl)
            ]
            : [];
    }

    public static EmailSenderProfile SelectSender(IReadOnlyList<EmailSenderProfile> senders, int campaignId, string recipientAddress, int recipientIndex)
    {
        if (senders.Count == 0)
            throw new InvalidOperationException("At least one configured sender profile is required.");

        if (senders.Count == 1)
            return senders[0];

        var seed = $"{campaignId}:{recipientAddress.Trim().ToUpperInvariant()}:{recipientIndex}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var value = BitConverter.ToUInt32(hash, 0);
        return senders[(int)(value % (uint)senders.Count)];
    }

    private static EmailSenderProfile? FromProfile(EmailSenderProfileOptions profile, EmailNotificationOptions fallback)
    {
        var fromEmail = Clean(profile.FromEmail);
        var password = Clean(profile.Password) ?? Clean(fallback.Password);
        var smtpServer = Clean(profile.SmtpServer) ?? Clean(fallback.SmtpServer);
        var smtpPort = profile.SmtpPort ?? fallback.SmtpPort;

        if (fromEmail is null ||
            password is null ||
            smtpServer is null ||
            smtpPort <= 0)
        {
            return null;
        }

        return new EmailSenderProfile(
            Clean(profile.Name) ?? fromEmail,
            Clean(profile.FromName) ?? Clean(fallback.FromName) ?? "Dealvert",
            fromEmail,
            Clean(profile.Username) ?? fromEmail,
            password,
            smtpServer,
            smtpPort,
            profile.EnableSsl ?? fallback.EnableSsl);
    }

    private static bool LegacyProfileConfigured(EmailNotificationOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.FromEmail) &&
            !string.IsNullOrWhiteSpace(options.Password) &&
            !string.IsNullOrWhiteSpace(options.SmtpServer) &&
            options.SmtpPort > 0;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record EmailSenderProfile(
    string Name,
    string FromName,
    string FromEmail,
    string Username,
    string Password,
    string SmtpServer,
    int SmtpPort,
    bool EnableSsl);
