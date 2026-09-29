using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;

namespace TimeBank.API.Controllers;

[ApiController]
[Route("api/[controller]")] // ruta: /api/users
public class UsersController : ControllerBase
{
    // Horas de bienvenida que recibe cada usuario nuevo para poder empezar a intercambiar
    private const decimal WelcomeHours = 2;

    private readonly TimeBankDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public UsersController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/users?search=ana
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] string? search)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(text)
                                  || u.LastName.ToLower().Contains(text)
                                  || u.Email.ToLower().Contains(text));
        }

        var users = await query.OrderBy(u => u.LastName).ToListAsync();
        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }
        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        var emailInUse = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (emailInUse)
        {
            return BadRequest(new { message = "Ya existe un usuario con ese correo" });
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            HoursBalance = WelcomeHours,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password); // la contraseña se guarda encriptada

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // PUT: api/users/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var emailInUse = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.ToLower());
        if (emailInUse)
        {
            return BadRequest(new { message = "Ya existe otro usuario con ese correo" });
        }

        // El saldo de horas NO se edita aquí: solo cambia con los intercambios
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        // Regla: si ya participó en servicios o solicitudes, se desactiva en lugar de borrarse,
        // para no perder el historial de horas
        var hasHistory = await _context.Services.AnyAsync(s => s.UserId == id)
                      || await _context.ServiceRequests.AnyAsync(r => r.RequesterId == id);
        if (hasHistory)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "El usuario tiene historial, así que se desactivó en lugar de eliminarse" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
