using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Models;
using TimeBank.API.Security;

namespace TimeBank.API.Controllers;

// Cualquier usuario con sesión ve las categorías; solo el Administrador las crea, edita o elimina
[Route("api/[controller]")] // ruta: /api/categories
public class CategoriesController : ApiControllerBase
{
    private readonly TimeBankDbContext _context;

    public CategoriesController(TimeBankDbContext context)
    {
        _context = context;
    }

    // GET: api/categories (público: cualquiera puede ver las categorías, aunque no haya iniciado sesión)
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        return Ok(categories);
    }

    // GET: api/categories/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new { message = "Categoría no encontrada" });
        }
        return Ok(category);
    }

    // POST: api/categories
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return BadRequest(new { message = "El nombre de la categoría es obligatorio" });
        }

        var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());
        if (exists)
        {
            return BadRequest(new { message = "Ya existe una categoría con ese nombre" });
        }

        category.Id = 0; // el Id lo genera la base de datos
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    // PUT: api/categories/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.Id)
        {
            return BadRequest(new { message = "El Id de la URL no coincide con el Id de la categoría" });
        }

        var existing = await _context.Categories.FindAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Categoría no encontrada" });
        }

        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return BadRequest(new { message = "El nombre de la categoría es obligatorio" });
        }

        existing.Name = category.Name;
        existing.Description = category.Description;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new { message = "Categoría no encontrada" });
        }

        // Regla: no se borra una categoría que tiene servicios
        var hasServices = await _context.Services.AnyAsync(s => s.CategoryId == id);
        if (hasServices)
        {
            return BadRequest(new { message = "No se puede eliminar: la categoría tiene servicios registrados" });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
