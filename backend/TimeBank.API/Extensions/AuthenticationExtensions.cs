using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TimeBank.API.Models;
using TimeBank.API.Services;
using TimeBank.API.Settings;

namespace TimeBank.API.Extensions;

// Toda la configuración del login en un solo lugar, para que Program.cs quede limpio
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtSettings.SectionName);
        var settings = section.Get<JwtSettings>()
            ?? throw new InvalidOperationException("Falta la sección \"Jwt\" en appsettings.json");

        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < 32)
        {
            throw new InvalidOperationException("La clave Jwt:Key debe tener al menos 32 caracteres");
        }

        // Opciones disponibles por inyección de dependencias (IOptions<JwtSettings>)
        services.Configure<JwtSettings>(section);

        // Servicios del módulo de login
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // mantiene los nombres "sub", "email", "jti"
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = TokenService.RoleClaimType // así funciona [Authorize(Roles = "Administrador")]
                };

                options.Events = new JwtBearerEvents
                {
                    // Después de validar la firma, se revisa en la base que la sesión siga abierta
                    OnTokenValidated = async context =>
                    {
                        var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        if (tokenId == null || !await authService.IsSessionActiveAsync(tokenId))
                        {
                            context.Fail("La sesión fue cerrada o ya expiró");
                        }
                    },
                    // Respuesta 401 en español y en el mismo formato { message } que el resto de la API
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var message = context.AuthenticateFailure?.Message == "La sesión fue cerrada o ya expiró"
                            ? "La sesión fue cerrada o ya expiró. Vuelve a iniciar sesión"
                            : "Debes iniciar sesión: falta el token o no es válido";
                        await context.Response.WriteAsJsonAsync(new { message });
                    },
                    // 403: el token es válido pero el rol no alcanza (por ejemplo, un Usuario en una ruta de Administrador)
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { message = "No tienes permiso para realizar esta acción" });
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
