using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Controladores de la API (CRUD)
builder.Services.AddControllers(options =>
    {
        // Evita que las propiedades de navegación (User, Service...) se pidan como obligatorias
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // Evita el error de "ciclo" al devolver objetos relacionados (servicio -> usuario -> servicios...)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

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
else
{
    app.UseHttpsRedirection();
}

// Activa las rutas de los controladores: /api/categories, /api/users, /api/services...
app.MapControllers();

app.Run();
