# Módulo de login de TimeBank

Login con **token JWT**, **roles** (Administrador / Usuario), **contraseña segura** y
**sesiones guardadas en la base de datos** (tabla `UserSessions`). Todo el CRUD pide token.
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

## Roles y permisos

| Rol | Cómo se obtiene | Qué puede hacer |
|---|---|---|
| **Usuario** | Al registrarse en `/api/auth/register` (automático) | Ver categorías, servicios y perfiles; publicar, editar y borrar **sus** servicios; pedir servicios **a su nombre**; aceptar o rechazar solicitudes de **sus** servicios; marcar como completadas **sus** solicitudes; valorar; ver **su** historial de horas |
| **Administrador** | Lo crea otro Administrador (`POST /api/users` o `PUT /api/users/{id}/roles`) | Todo lo anterior sobre cualquier registro, más: crear/editar/eliminar categorías, listar/crear/editar/eliminar usuarios, ver y asignar roles |

Administrador inicial (lo crea la migración `AddRoles`): **admin@timebank.com / Admin2026\***.
Nadie puede registrarse como Administrador por su cuenta.

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/categories`, `/api/services`, `/api/servicerequests`, `/api/reviews`, `/api/users/{id}` | Cualquier usuario con sesión |
| POST / PUT / DELETE | `/api/categories` | Administrador |
| GET / POST / PUT / DELETE | `/api/users`, `PUT /api/users/{id}/roles`, `GET /api/roles` | Administrador |
| POST / PUT / DELETE | `/api/services` | Dueño del servicio o Administrador |
| POST / PUT / DELETE | `/api/servicerequests` | Quien pide el servicio o Administrador |
| PUT | `/api/servicerequests/{id}/status` → Aceptada / Rechazada | Quien ofrece el servicio o Administrador |
| PUT | `/api/servicerequests/{id}/status` → Completada | Quien pidió el servicio o Administrador |
| POST / DELETE | `/api/reviews` | Quien valora o Administrador |
| GET | `/api/timetransactions` | Cada usuario ve sus movimientos; el Administrador, los de cualquiera |

## Contraseña segura

Mínimo **8 caracteres**, con **una mayúscula, una minúscula, un número y un carácter especial**, sin espacios
(atributo `[StrongPassword]` en `Security/StrongPasswordAttribute.cs`). Se valida al registrarse, al crear
usuarios y al cambiar la contraseña. Ejemplo válido: `Clave2026*`.

Códigos: **401** = falta el token, no es válido o la sesión se cerró; **403** = usuario desactivado;
**409** = el correo ya está registrado. En el CRUD, **403** = tiene sesión pero no permiso
(por ejemplo, un Usuario que intenta crear una categoría o editar el servicio de otro).

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
├── Models/Role.cs / UserRole.cs          ← roles y relación usuario-rol (muchos a muchos)
├── Security/AppRoles.cs                  ← nombres de los roles
├── Security/StrongPasswordAttribute.cs   ← validación de contraseña segura
├── Controllers/ApiControllerBase.cs      ← [Authorize] para todo el CRUD + ayuda de permisos
├── Controllers/RolesController.cs        ← GET /api/roles
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

- `AddUserSessions`: crea la tabla `UserSessions` y agrega la columna `LastLoginAt` a `Users`.
- `AddRoles`: crea `Roles` (Administrador, Usuario) y `UserRoles`, crea el Administrador inicial y
  da el rol Usuario a los usuarios que ya existían.

```
cd backend\TimeBank.API
dotnet ef database update
```

```sql
SELECT * FROM "__EFMigrationsHistory";
SELECT "Id", "UserId", "CreatedAt", "ExpiresAt", "RevokedAt", "UserAgent" FROM "UserSessions" ORDER BY "Id" DESC;
SELECT "Id", "Email", "LastLoginAt" FROM "Users" ORDER BY "Id" DESC;
SELECT u."Id", u."Email", r."Name" AS "Rol"
FROM "Users" u JOIN "UserRoles" ur ON ur."UserId" = u."Id" JOIN "Roles" r ON r."Id" = ur."RoleId"
ORDER BY u."Id";
```

## Pruebas

- Postman: carpeta **0. Inicio de sesión** (guarda `{{adminToken}}`, que usan las carpetas 1 a 5) y carpeta **6. Login**.
- Scripts: `tests\probar-api.ps1` (43 pruebas del CRUD con token y roles) y `tests\probar-login.ps1` (22 pruebas del login).
- REST Client: sección LOGIN al final de `backend/TimeBank.API/TimeBank.API.http`.
