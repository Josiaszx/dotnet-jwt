using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Options;

public class JwtSettings
{
    // Debe coincidir con el nombre de la sección en appsettings.json.
    public const string SectionName = "JwtSettings";

    // Quién emite el token (esta API).
    [Required] public string Issuer { get; set; } = string.Empty;

    // Para quién es el token (los clientes que lo consumen)
    [Required] public string Audience { get; set; } = string.Empty;

    // Clave secreta con la que se FIRMA el token (HS256 pide >= 32 caracteres).
    // Viene de user-secrets / variables de entorno, nunca de appsettings.json.
    [Required, MinLength(32)] public string SecretKey { get; set; } = string.Empty;

    // El access token vive poco: si se roba, el daño es limitado.
    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 15;

    // El refresh token vive más y se guarda en la BD (hasheado).
    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;
}
