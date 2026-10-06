using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeBank.API.Extensions;
using TimeBank.API.Security;

namespace TimeBank.API.Controllers;

// Base de los controladores del CRUD. [Authorize] hace que TODAS sus rutas pidan el token;
// si no se envía o no es válido, la API responde 401.
[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    // Id del usuario que inició sesión (viene dentro del token)
    protected int CurrentUserId => User.GetUserId();

    protected bool IsAdmin => User.IsInRole(AppRoles.Admin);

    // El administrador puede todo; un usuario solo puede actuar sobre lo suyo
    protected bool CanActAs(int userId) => IsAdmin || userId == CurrentUserId;

    // 403: está autenticado, pero no tiene permiso para esta acción
    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });
}
