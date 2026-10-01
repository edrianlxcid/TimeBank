using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeBank.API.Dtos;
using TimeBank.API.Extensions;
using TimeBank.API.Services;

namespace TimeBank.API.Controllers;

// Módulo de login. Las rutas marcadas con [Authorize] necesitan el token:
// en Postman va en Authorization -> Bearer Token (o en el header "Authorization: Bearer <token>")
[ApiController]
[Route("api/[controller]")] // ruta: /api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST: api/auth/register  -> crea la cuenta (recibe 2 horas de bienvenida)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.Success) return ToErrorResponse(result);

        return CreatedAtAction(nameof(GetProfile), null, result.Value);
    }

    // POST: api/auth/login  -> devuelve el token
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(dto, ip, userAgent);
        if (!result.Success) return ToErrorResponse(result);

        return Ok(result.Value);
    }

    // GET: api/auth/me  -> datos del usuario que inició sesión
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var result = await _authService.GetProfileAsync(User.GetUserId());
        if (!result.Success) return ToErrorResponse(result);

        return Ok(result.Value);
    }

    // PUT: api/auth/me  -> editar nombre, apellido y teléfono
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileDto dto)
    {
        var result = await _authService.UpdateProfileAsync(User.GetUserId(), dto);
        if (!result.Success) return ToErrorResponse(result);

        return Ok(result.Value);
    }

    // PUT: api/auth/change-password  -> cambia la contraseña y cierra las otras sesiones
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var result = await _authService.ChangePasswordAsync(User.GetUserId(), User.GetTokenId(), dto);
        if (!result.Success) return ToErrorResponse(result);

        return Ok(new { message = result.Value });
    }

    // GET: api/auth/sessions  -> historial de inicios de sesión del usuario
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> GetSessions()
    {
        var result = await _authService.GetSessionsAsync(User.GetUserId(), User.GetTokenId());
        return Ok(result.Value);
    }

    // DELETE: api/auth/logout  -> cierra la sesión actual (el token deja de servir)
    [HttpDelete("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync(User.GetTokenId());
        if (!result.Success) return ToErrorResponse(result);

        return NoContent();
    }

    // DELETE: api/auth/sessions/5  -> cierra otra sesión propia (por ejemplo, la de otro computador)
    [HttpDelete("sessions/{id:int}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(int id)
    {
        var result = await _authService.RevokeSessionAsync(User.GetUserId(), id);
        if (!result.Success) return ToErrorResponse(result);

        return NoContent();
    }

    // Traduce el tipo de error del servicio al código HTTP que corresponde
    private ObjectResult ToErrorResponse<T>(ServiceResult<T> result)
    {
        var status = result.ErrorType switch
        {
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { message = result.Error });
    }
}
