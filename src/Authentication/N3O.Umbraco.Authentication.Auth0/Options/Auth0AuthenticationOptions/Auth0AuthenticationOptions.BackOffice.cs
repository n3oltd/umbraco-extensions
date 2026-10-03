namespace N3O.Umbraco.Authentication.Auth0.Options;

public class Auth0BackOfficeAuthenticationOptions {
    public Auth0AuthenticationOptions Auth0 { get; set; }
    public bool AutoRedirect { get; set; } = true;
}