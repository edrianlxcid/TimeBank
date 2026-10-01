namespace TimeBank.API.Settings;

// Configuración del token JWT. Se llena desde la sección "Jwt" de appsettings.json
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;   // quién emite el token (la API)
    public string Audience { get; set; } = string.Empty; // para quién es el token (el frontend)
    public string Key { get; set; } = string.Empty;      // clave secreta para firmar (mínimo 32 caracteres)
    public int ExpirationMinutes { get; set; } = 120;    // cuánto dura la sesión
}
