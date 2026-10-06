using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;

namespace TimeBank.API.Controllers;

// Todos con sesión ven los servicios; cada usuario crea, edita o elimina solo los suyos (el Administrador, cualquiera)
[Route("api/[controller]")] // ruta: /api/services
public class ServicesController : ApiControllerBase
{
    private readonly TimeBankDbContext _context;

    public ServicesController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/services?categoryId=2&search=ingles&onlyActive=true
    // Búsqueda y filtros que pide el proyecto
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Service>>> GetServices(
        [FromQuery] int? categoryId, [FromQuery] int? userId,
        [FromQuery] string? search, [FromQuery] bool onlyActive = false)
    {
        var query = _context.Services
            .Include(s => s.User)
            .Include(s => s.Category)
            .AsQueryable();

        if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId);
        if (userId.HasValue) query = query.Where(s => s.UserId == userId);
        if (onlyActive) query = query.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(s => s.Title.ToLower().Contains(text) || s.Description.ToLower().Contains(text));
        }

        var services = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return Ok(services);
    }

    // GET: api/services/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Service>> GetService(int id)
    {
        var service = await _context.Services
            .Include(s => s.User)
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound(new { message = "Servicio no encontrado" });
        }
        return Ok(service);
    }

    // POST: api/services
    [HttpPost]
    public async Task<ActionResult<Service>> CreateService(ServiceDto dto)
    {
        if (!CanActAs(dto.UserId))
        {
            return Forbidden("Solo puedes publicar servicios a tu nombre");
        }

        var error = await ValidateAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        var service = new Service
        {
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            EstimatedHours = dto.EstimatedHours,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
    }

    // PUT: api/services/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateService(int id, ServiceDto dto)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound(new { message = "Servicio no encontrado" });
        }

        if (!CanActAs(service.UserId) || !CanActAs(dto.UserId))
        {
            return Forbidden("Solo puedes editar tus propios servicios");
        }

        var error = await ValidateAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        service.UserId = dto.UserId;
        service.CategoryId = dto.CategoryId;
        service.Title = dto.Title;
        service.Description = dto.Description;
        service.EstimatedHours = dto.EstimatedHours;
        service.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/services/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound(new { message = "Servicio no encontrado" });
        }

        if (!CanActAs(service.UserId))
        {
            return Forbidden("Solo puedes eliminar tus propios servicios");
        }

        // Regla: si ya tiene solicitudes, se desactiva para no perder el historial
        var hasRequests = await _context.ServiceRequests.AnyAsync(r => r.ServiceId == id);
        if (hasRequests)
        {
            service.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "El servicio tiene solicitudes, así que se desactivó en lugar de eliminarse" });
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Validaciones comunes para crear y editar
    private async Task<string?> ValidateAsync(ServiceDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return "El usuario especificado no existe";
        if (!user.IsActive) return "El usuario está inactivo";

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists) return "La categoría especificada no existe";

        return null;
    }
}
