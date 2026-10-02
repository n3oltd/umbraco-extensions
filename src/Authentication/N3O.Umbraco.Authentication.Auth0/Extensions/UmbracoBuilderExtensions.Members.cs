using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using N3O.Umbraco.Authentication.Auth0.Options;
using N3O.Umbraco.Authentication.Extensions;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;

namespace N3O.Umbraco.Authentication.Auth0.Extensions;

public static partial class UmbracoBuilderExtensions {
    private static readonly string Name = "name";
    private static readonly string Nickname = "nickname";
    
    public static IUmbracoBuilder AddAuth0MemberExternalLogins(this IUmbracoBuilder builder,
                                                               Action<MemberExternalLoginProviderOptions> configure = null) {
        if (!builder.Config.GetMembersAuthenticationSection().Exists()) {
            return builder;
        }

        if (configure != null) {
            builder.Services.AddSingleton(configure);
        }

        builder.Services.ConfigureOptions<Auth0MemberLoginProviderOptions>();

        builder.AddMemberExternalLogins(logins => {
            logins.AddMemberLogin(
                backOfficeAuthenticationBuilder => {
                    var auth0Settings = builder.Config
                                               .GetMembersAuthenticationSection()
                                               .Get<Auth0MemberAuthenticationOptions>();
                    
                    backOfficeAuthenticationBuilder.AddOpenIdConnect(
                        backOfficeAuthenticationBuilder.SchemeForMembers(Auth0MemberLoginProviderOptions.SchemeName), opt => {
                            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                            opt.Authority = auth0Settings.Auth0.Login.Authority;
                            opt.ClientId = auth0Settings.Auth0.Login.ClientId;
                            opt.ClientSecret = auth0Settings.Auth0.Login.ClientSecret;
                            opt.ResponseType = OpenIdConnectResponseType.Code;
                            opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                            opt.CallbackPath = "/signin-oidc";
                            opt.TokenValidationParameters.NameClaimType = ClaimTypes.Name;
                            opt.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                            opt.RequireHttpsMetadata = true;
                            opt.GetClaimsFromUserInfoEndpoint = true;
                            opt.SaveTokens = true;
                            opt.UsePkce = true;

                            opt.Scope.Add("openid");
                            opt.Scope.Add("email");
                            opt.Scope.Add("profile");
                            
                            OnTokenValidated(opt);
                        });
                });
        });

        return builder;
    }

    private static void OnTokenValidated(OpenIdConnectOptions options) {
        options.Events.OnTokenValidated = context => {
            var claims = context?.Principal?.Claims.ToList();
            var name = claims?.SingleOrDefault(x => x.Type == Name) ?? 
                       claims?.SingleOrDefault(x => x.Type == Nickname);
                                
            if (name != null) {
                claims.Add(new Claim(ClaimTypes.Name, name.Value));
            }

            if (context != null) {
                var authenticationType = context.Principal?.Identity?.AuthenticationType;
                                    
                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
            }

            return Task.CompletedTask;
        };
    }
}
