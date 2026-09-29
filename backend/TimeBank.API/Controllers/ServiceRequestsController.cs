using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;

namespace TimeBank.API.Controllers;

[ApiController]
[Route("api/[controller]")] // ruta: /api/servicerequests
public class ServiceRequestsController : ControllerBase
{
    // Estados posibles de una solicitud
    private const string StatusPending = "Pendiente";
    private const string StatusAccepted = "Aceptada";
    private const string StatusRejected = "Rechazada";
    private const string StatusCompleted = "Completada";

    private readonly TimeBankDbContext _context;

    public ServiceRequestsController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/servicerequests?status=Pendiente&requesterId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetRequests(
        [FromQuery] string? status, [FromQuery] int? requesterId, [FromQuery] int? serviceId)
    {
        var query = _context.ServiceRequests
            .Include(r => r.Service)
            .Include(r => r.Requester)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        if (requesterId.HasValue) query = query.Where(r => r.RequesterId == requesterId);
        if (serviceId.HasValue) query = query.Where(r => r.ServiceId == serviceId);

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(requests);
    }

    // GET: api/servicerequests/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequest>> GetRequest(int id)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Service)
            .Include(r => r.Requester)
            .Include(r => r.Transaction)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound(new { message = "Solicitud no encontrada" });
        }
        return Ok(request);
    }

    // POST: api/servicerequests
    [HttpPost]
    public async Task<ActionResult<ServiceRequest>> CreateRequest(CreateServiceRequestDto dto)
    {
        var service = await _context.Services.FindAsync(dto.ServiceId);
        if (service == null) return BadRequest(new { message = "El servicio especificado no existe" });
        if (!service.IsActive) return BadRequest(new { message = "El servicio no está disponible" });

        var requester = await _context.Users.FindAsync(dto.RequesterId);
        if (requester == null) return BadRequest(new { message = "El usuario que solicita no existe" });
        if (!requester.IsActive) return BadRequest(new { message = "El usuario está inactivo" });

        // Regla: nadie puede pedir su propio servicio
        if (service.UserId == dto.RequesterId)
        {
            return BadRequest(new { message = "No puedes solicitar tu propio servicio" });
        }

        // Regla: no se pueden pedir más horas de las que se tienen de saldo
        if (dto.RequestedHours > requester.HoursBalance)
        {
            return BadRequest(new { message = $"Saldo insuficiente: tienes {requester.HoursBalance} horas y pides {dto.RequestedHours}" });
        }

        var request = new ServiceRequest
        {
            ServiceId = dto.ServiceId,
            RequesterId = dto.RequesterId,
            Message = dto.Message,
            RequestedHours = dto.RequestedHours,
            Status = StatusPending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
    }

    // PUT: api/servicerequests/5
    // Edita el mensaje y las horas, solo mientras la solicitud está Pendiente
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRequest(int id, CreateServiceRequestDto dto)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound(new { message = "Solicitud no encontrada" });

        if (request.Status != StatusPending)
        {
            return BadRequest(new { message = $"Solo se puede editar una solicitud Pendiente (esta está {request.Status})" });
        }

        var requester = await _context.Users.FindAsync(request.RequesterId);
        if (requester != null && dto.RequestedHours > requester.HoursBalance)
        {
            return BadRequest(new { message = $"Saldo insuficiente: tienes {requester.HoursBalance} horas" });
        }

        request.Message = dto.Message;
        request.RequestedHours = dto.RequestedHours;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/servicerequests/5/status
    // Cambia el estado respetando el flujo: Pendiente -> Aceptada/Rechazada, Aceptada -> Completada
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto dto)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound(new { message = "Solicitud no encontrada" });

        var allowed = request.Status switch
        {
            StatusPending => new[] { StatusAccepted, StatusRejected },
            StatusAccepted => new[] { StatusCompleted },
            _ => Array.Empty<string>()
        };

        if (!allowed.Contains(dto.Status))
        {
            return BadRequest(new
            {
                message = $"No se puede pasar de {request.Status} a {dto.Status}",
                allowedStatuses = allowed
            });
        }

        // Al completar, se transfieren las horas: se restan a quien recibió el servicio
        // y se suman a quien lo prestó
        if (dto.Status == StatusCompleted)
        {
            var requester = await _context.Users.FindAsync(request.RequesterId);
            var provider = await _context.Users.FindAsync(request.Service.UserId);
            if (requester == null || provider == null)
            {
                return BadRequest(new { message = "No se encontró a uno de los usuarios del intercambio" });
            }

            if (requester.HoursBalance < request.RequestedHours)
            {
                return BadRequest(new { message = "El usuario ya no tiene saldo suficiente para completar el intercambio" });
            }

            requester.HoursBalance -= request.RequestedHours;
            provider.HoursBalance += request.RequestedHours;

            _context.TimeTransactions.Add(new TimeTransaction
            {
                ServiceRequestId = request.Id,
                FromUserId = requester.Id,
                ToUserId = provider.Id,
                Hours = request.RequestedHours,
                CreatedAt = DateTime.UtcNow
            });
        }

        request.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Solicitud actualizada a {dto.Status}", request.Id, request.Status });
    }

    // DELETE: api/servicerequests/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound(new { message = "Solicitud no encontrada" });

        // Regla: solo se puede cancelar mientras está Pendiente
        if (request.Status != StatusPending)
        {
            return BadRequest(new { message = $"Solo se puede eliminar una solicitud Pendiente (esta está {request.Status})" });
        }

        _context.ServiceRequests.Remove(request);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
