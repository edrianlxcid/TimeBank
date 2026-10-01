using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TimeBank.API.Extensions;

// Atajos para leer los datos del usuario que vienen dentro del token
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? throw new InvalidOperationException("El token no tiene el Id del usuario"));

    public static string GetTokenId(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Jti)
        ?? throw new InvalidOperationException("El token no tiene identificador de sesión");
}
