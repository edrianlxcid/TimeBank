using TimeBank.API.Models;

namespace TimeBank.API.Services;

// Genera los tokens JWT. Se separa en su propia clase para poder cambiar la forma
// de crear tokens sin tocar la lógica del login.
public interface ITokenService
{
    // Devuelve el token, su identificador (jti) y la fecha en que vence.
    // Los roles se guardan dentro del token como claims de tipo "role".
    (string Token, string TokenId, DateTime ExpiresAt) CreateToken(User user, IEnumerable<string> roles);
}
