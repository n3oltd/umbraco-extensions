using Auth0.ManagementApi;
using N3O.Umbraco.Authentication.Auth0.Lookups;
using N3O.Umbraco.Extensions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Authentication.Auth0;

internal static class Auth0UserMapping {
    public static async Task<Auth0User> ToAuth0UserAsync(this UserResponseSchema user,
                                                         IUserDirectoryConnections userDirectoryConnections,
                                                         UserDirectoryType directoryType) {
        if (!user.HasValue()) {
            return null;
        }

        return await MapAsync(userDirectoryConnections,
                              user.UserId,
                              user.Email,
                              user.Picture,
                              user.GivenName,
                              user.FamilyName,
                              user.EmailVerified,
                              user.Blocked,
                              user.Identities,
                              user.LastLogin,
                              user.LastIp,
                              directoryType);
    }

    public static async Task<Auth0User> ToAuth0UserAsync(this CreateUserResponseContent user,
                                                         IUserDirectoryConnections userDirectoryConnections,
                                                         UserDirectoryType directoryType) {
        if (!user.HasValue()) {
            return null;
        }

        return await MapAsync(userDirectoryConnections,
                              user.UserId,
                              user.Email,
                              user.Picture,
                              user.GivenName,
                              user.FamilyName,
                              user.EmailVerified,
                              user.Blocked,
                              user.Identities,
                              user.LastLogin,
                              user.LastIp,
                              directoryType);
    }
    
    private static async Task<Auth0User> MapAsync(IUserDirectoryConnections userDirectoryConnections,
                                                  string userId,
                                                  string email,
                                                  string picture,
                                                  string givenName,
                                                  string familyName,
                                                  bool? emailVerified,
                                                  bool? blocked,
                                                  IEnumerable<UserIdentitySchema> identities,
                                                  DateTime? lastLogin,
                                                  string lastIp,
                                                  UserDirectoryType directoryType) {
        var user = new Auth0User();
        user.Id = userId;
        user.Email = email;
        user.Picture = picture;
        user.FirstName = givenName;
        user.LastName = familyName;
        user.EmailVerified = emailVerified == true;
        user.Blocked = blocked == true;
        user.IsFederated = await userDirectoryConnections.IsFederatedByEmailAsync(directoryType, user.Email);
        user.LastLogin = ParseLastLogin(lastLogin);
        user.LastIpAddress = lastIp;
        user.Identities = identities;

        return user;
    }
    
    private static Instant? ParseLastLogin(DateTime? lastLogin) {
        if (lastLogin == null) {
            return null;
        }

        return Instant.FromDateTimeOffset(new DateTimeOffset(lastLogin.Value));
    }
}
