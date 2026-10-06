using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;

namespace TimeBank.API.Controllers;

// Valoraciones: cada usuario valora a su nombre; las borra quien la hizo o el Administrador
[Route("api/[controller]")] // ruta: /api/reviews
public class ReviewsController : ApiControllerBase
{
    private readonly TimeBankDbContext _context;

    public ReviewsController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/reviews?userId=2  (valoraciones que recibió un usuario)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews([FromQuery] int? userId)
    {
        var query = _context.Reviews.AsQueryable();
        if (userId.HasValue) query = query.Where(r => r.ReviewedUserId == userId);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(reviews);
    }

    // GET: api/reviews/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Review>> GetReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound(new { message = "Valoración no encontrada" });
        return Ok(review);
    }

    // POST: api/reviews
    [HttpPost]
    public async Task<ActionResult<Review>> CreateReview(CreateReviewDto dto)
    {
        if (!CanActAs(dto.ReviewerId))
        {
            return Forbidden("Solo puedes valorar a tu nombre");
        }

        var request = await _context.ServiceRequests
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == dto.ServiceRequestId);
        if (request == null) return BadRequest(new { message = "La solicitud especificada no existe" });

        // Regla: solo se valora un intercambio completado
        if (request.Status != "Completada")
        {
            return BadRequest(new { message = "Solo se puede valorar un intercambio Completado" });
        }

        // Solo participan quien pidió y quien prestó el servicio; cada uno valora al otro
        int reviewedUserId;
        if (dto.ReviewerId == request.RequesterId) reviewedUserId = request.Service.UserId;
        else if (dto.ReviewerId == request.Service.UserId) reviewedUserId = request.RequesterId;
        else return BadRequest(new { message = "Solo los participantes del intercambio pueden valorarlo" });

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.ServiceRequestId == dto.ServiceRequestId && r.ReviewerId == dto.ReviewerId);
        if (alreadyReviewed)
        {
            return BadRequest(new { message = "Ya valoraste este intercambio" });
        }

        var review = new Review
        {
            ServiceRequestId = dto.ServiceRequestId,
            ReviewerId = dto.ReviewerId,
            ReviewedUserId = reviewedUserId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
    }

    // DELETE: api/reviews/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound(new { message = "Valoración no encontrada" });

        if (!CanActAs(review.ReviewerId))
        {
            return Forbidden("Solo puedes eliminar tus propias valoraciones");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
