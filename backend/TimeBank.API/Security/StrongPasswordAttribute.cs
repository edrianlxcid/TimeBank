using System.ComponentModel.DataAnnotations;

namespace TimeBank.API.Security;

// Validación de contraseña segura: mínimo 8 caracteres, una mayúscula, una minúscula,
// un número y un carácter especial. Se usa como atributo en los DTOs: [StrongPassword]
[AttributeUsage(AttributeTargets.Property)]
public class StrongPasswordAttribute : ValidationAttribute
{
    public const int MinLength = 8;
    public const int MaxLength = 64;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Si viene vacía, de eso se encarga [Required]
        if (value is not string password || password.Length == 0)
        {
            return ValidationResult.Success;
        }

        var missing = new List<string>();
        if (password.Length < MinLength) missing.Add($"al menos {MinLength} caracteres");
        if (!password.Any(char.IsUpper)) missing.Add("una letra mayúscula");
        if (!password.Any(char.IsLower)) missing.Add("una letra minúscula");
        if (!password.Any(char.IsDigit)) missing.Add("un número");
        if (!password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) missing.Add("un carácter especial (por ejemplo ! @ # $ % * .)");
        if (password.Any(char.IsWhiteSpace)) missing.Add("no tener espacios");
        if (password.Length > MaxLength) missing.Add($"máximo {MaxLength} caracteres");

        if (missing.Count == 0)
        {
            return ValidationResult.Success;
        }

        // "una mayúscula, un número y un carácter especial"
        var text = missing.Count == 1
            ? missing[0]
            : string.Join(", ", missing.Take(missing.Count - 1)) + " y " + missing[^1];
        return new ValidationResult("La contraseña debe tener " + text, [validationContext.MemberName ?? "Password"]);
    }
}
