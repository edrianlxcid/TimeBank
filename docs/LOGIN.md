# Módulo de login de TimeBank

Login con **token JWT** y **sesiones guardadas en la base de datos** (tabla `UserSessions`).
Al iniciar sesión, la API devuelve un token. Ese token se envía en cada petición protegida
con el header `Authorization: Bearer <token>`. Al cerrar sesión, el token deja de servir.

## Endpoints (`/api/auth`)

| Método | Ruta | Token | Qué hace | Respuestas |
|---|---|---|---|---|
| POST | `/api/auth/register` | No | Crea la cuenta (2 horas de bienvenida) | 201, 400, 409 |
| POST | `/api/auth/login` | No | Devuelve el token y los datos del usuario | 200, 400, 401, 403 |
| GET | `/api/auth/me` | Sí | Perfil del usuario que inició sesión | 200, 401 |
| PUT | `/api/auth/me` | Sí | Edita nombre, apellido y teléfono | 200, 400, 401 |
| PUT | `/api/auth/change-password` | Sí | Cambia la contraseña y cierra las otras sesiones | 200, 400, 401 |
| GET | `/api/auth/sessions` | Sí | Historial de sesiones (activa/cerrada, actual) | 200, 401 |
| DELETE | `/api/auth/sessions/{id}` | Sí | Cierra otra sesión propia | 204, 400, 401, 404 |
| DELETE | `/api/auth/logout` | Sí | Cierra la sesión actual | 204, 401 |

Códigos: **401** = falta el token, no es válido o la sesión se cerró; **403** = usuario desactivado;
**409** = el correo ya está registrado.

## Cómo está organizado (buenas prácticas)

```
backend/TimeBank.API/
├── Controllers/AuthController.cs        ← solo recibe la petición y devuelve la respuesta
├── Services/
│   ├── IAuthService.cs / AuthService.cs  ← reglas del login (registro, login, sesiones, contraseña)
│   ├── ITokenService.cs / TokenService.cs← genera el token JWT
│   └── ServiceResult.cs                  ← resultado ok/error sin depender de HTTP
├── Dtos/AuthDtos.cs                      ← datos que entran y salen (con validaciones)
├── Models/UserSession.cs                 ← tabla de sesiones
├── Settings/JwtSettings.cs               ← configuración "Jwt" de appsettings.json
└── Extensions/
    ├── AuthenticationExtensions.cs       ← registra JWT, servicios y valida la sesión en cada petición
    └── ClaimsPrincipalExtensions.cs      ← lee el Id del usuario y de la sesión desde el token
```

- **Interfaces + inyección de dependencias:** el controlador usa `IAuthService`, no la clase directamente.
- **DTOs:** nunca se expone el modelo `User` ni la contraseña.
- **Contraseña con hash** (`PasswordHasher<User>`), y el mismo mensaje para correo o contraseña incorrectos.
- **Options pattern:** la configuración del token se lee de `appsettings.json` → `JwtSettings`.
- **Sesiones en la base:** cada login crea una fila en `UserSessions`; el logout la marca con `RevokedAt`.
- **CORS** habilitado para el frontend Angular en `http://localhost:4200`.

## Base de datos

La migración `AddUserSessions` crea la tabla `UserSessions` y agrega la columna `LastLoginAt` a `Users`.

```
cd backend\TimeBank.API
dotnet ef database update
```

```sql
SELECT * FROM "__EFMigrationsHistory";
SELECT "Id", "UserId", "CreatedAt", "ExpiresAt", "RevokedAt", "UserAgent" FROM "UserSessions" ORDER BY "Id" DESC;
SELECT "Id", "Email", "LastLoginAt" FROM "Users" ORDER BY "Id" DESC;
```

## Pruebas

- Postman: carpeta **6. Login** de la colección `docs/TimeBank API` (el login guarda el token solo).
- Script: `powershell -ExecutionPolicy Bypass -File tests\probar-login.ps1` (21 pruebas).
- REST Client: sección LOGIN al final de `backend/TimeBank.API/TimeBank.API.http`.
