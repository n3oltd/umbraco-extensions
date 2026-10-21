using Microsoft.AspNetCore;
using Microsoft.Net.Http.Headers;
using OpenIddict.Server;
using System.Threading.Tasks;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.Hosting;

// The back office is a public client, so a Basic header on its requests is the browser replaying the staging
// login, which OpenIddict would otherwise read as the client's credentials and reject.
public class BackOfficeStagingCredentialsRemover :
    IOpenIddictServerHandler<OpenIddictServerEvents.ExtractTokenRequestContext>,
    IOpenIddictServerHandler<OpenIddictServerEvents.ExtractRevocationRequestContext> {
    public ValueTask HandleAsync(OpenIddictServerEvents.ExtractTokenRequestContext context) {
        RemoveStagingCredentials(context);

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(OpenIddictServerEvents.ExtractRevocationRequestContext context) {
        RemoveStagingCredentials(context);

        return ValueTask.CompletedTask;
    }

    private void RemoveStagingCredentials(OpenIddictServerEvents.BaseValidatingContext context) {
        if (context.Transaction.Request.ClientId == UmbracoConstants.OAuthClientIds.BackOffice) {
            context.Transaction.GetHttpRequest().Headers.Remove(HeaderNames.Authorization);
        }
    }
}
