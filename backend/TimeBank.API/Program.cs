using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Extensions;

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

// Login con token JWT: servicios, validación del token y sesiones (ver Extensions/AuthenticationExtensions.cs)
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS: permite que el frontend Angular (http://localhost:4200) llame a la API
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

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

app.UseCors(FrontendCorsPolicy);

// El orden importa: primero se identifica al usuario (token) y luego se revisan los permisos
app.UseAuthentication();
app.UseAuthorization();

// Activa las rutas de los controladores: /api/categories, /api/users, /api/services...
app.MapControllers();

app.Run();
