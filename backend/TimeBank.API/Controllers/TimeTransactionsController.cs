using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Models;

namespace TimeBank.API.Controllers;

// Historial de horas: solo lectura, porque las transacciones se crean
// automáticamente al completar una solicitud
[Route("api/[controller]")] // ruta: /api/timetransactions
public class TimeTransactionsController : ApiControllerBase
{
    private readonly TimeBankDbContext _context;

    public TimeTransactionsController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/timetransactions?userId=1  (movimientos donde participó el usuario)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimeTransaction>>> GetTransactions([FromQuery] int? userId)
    {
        // Un usuario normal solo ve sus propios movimientos; el Administrador puede ver los de cualquiera
        if (!IsAdmin) userId = CurrentUserId;

        var query = _context.TimeTransactions.AsQueryable();
        if (userId.HasValue) query = query.Where(t => t.FromUserId == userId || t.ToUserId == userId);

        var transactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(transactions);
    }
}
