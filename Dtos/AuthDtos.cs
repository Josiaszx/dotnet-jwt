using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Dtos;

// DTOs: lo que entra y sale por la API. Nunca exponemos las entidades (tienen PasswordHash).
// Con [ApiController], los atributos de validación se comprueban solos y devuelven 400 si fallan.
public record RegisterRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required] string Name,
    [Required] string Password);

public record RefreshRequest([Required] string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken);
