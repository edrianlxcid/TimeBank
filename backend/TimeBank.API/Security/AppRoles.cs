namespace TimeBank.API.Security;

// Nombres de los roles en un solo lugar, para no escribirlos a mano en cada controlador
public static class AppRoles
{
    // Administra la plataforma: categorías, usuarios y roles. Puede hacer todo.
    public const string Admin = "Administrador";

    // Rol por defecto de quien se registra: ofrece y pide servicios con sus horas
    public const string User = "Usuario";

    public static readonly string[] All = [Admin, User];
}
