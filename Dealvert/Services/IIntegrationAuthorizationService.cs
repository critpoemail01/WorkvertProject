using Alivert.Models;

namespace Alivert.Services;

public interface IIntegrationAuthorizationService
{
    PublicationAuthorization GetPostAuthorization(UserNotificationSettings? settings, string platform);
    PublicationAuthorization GetEmailAuthorization(UserNotificationSettings? settings);
}

public sealed record PublicationAuthorization(bool IsAuthorized, string Channel, string Detail);
