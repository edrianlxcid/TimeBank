using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Conexión a PostgreSQL con la cadena "DefaultConnection" de appsettings.Development.json
builder.Services.AddDbContext<TimeBankDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Prueba de conexión: devuelve las categorías guardadas en la base de datos
app.MapGet("/api/categories", async (TimeBankDbContext db) =>
    await db.Categories.ToListAsync())
.WithName("GetCategories");

app.Run();
