using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Security;

namespace TimeBank.API.Controllers;

// Consulta de roles (solo Administrador). Los roles se asignan con PUT /api/users/{id}/roles
[Route("api/[controller]")] // ruta: /api/roles
[Authorize(Roles = AppRoles.Admin)]
public class RolesController : ApiControllerBase
{
    private readonly TimeBankDbContext _context;

    public RolesController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/roles  -> roles con la cantidad de usuarios que tiene cada uno
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name, r.Description, Users = r.UserRoles.Count })
            .ToListAsync();
        return Ok(roles);
    }
}
