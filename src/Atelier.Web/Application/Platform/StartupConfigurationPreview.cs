using Atelier.Web.Data.Seed;
using Microsoft.Extensions.Configuration;

namespace Atelier.Web.Application.Platform;

public sealed record StartupConfigurationPreview(
    string ConnectionString,
    string SeededAdminEnterpriseWeChatId,
    string EnterpriseWeChatClientId,
    string EnterpriseWeChatClientSecret)
{
    public static StartupConfigurationPreview Build(IConfiguration? configuration = null)
    {
        var connectionString = configuration?["ATELIER_SQLITE_CONNECTION_STRING"];
        var enterpriseWeChatClientId = configuration?["ATELIER_ENTERPRISE_WECHAT_CLIENT_ID"];
        var enterpriseWeChatClientSecret = configuration?["ATELIER_ENTERPRISE_WECHAT_CLIENT_SECRET"];
        var blueprint = SeedData.BuildBlueprint(configuration);

        return new StartupConfigurationPreview(
            string.IsNullOrWhiteSpace(connectionString) ? "Data Source=atelier.db" : connectionString,
            blueprint.BootstrapAdminEnterpriseWeChatUserId,
            string.IsNullOrWhiteSpace(enterpriseWeChatClientId) ? "corp-id" : enterpriseWeChatClientId,
            string.IsNullOrWhiteSpace(enterpriseWeChatClientSecret) ? string.Empty : enterpriseWeChatClientSecret);
    }
}
