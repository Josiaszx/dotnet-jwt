using System.Security.Cryptography;
using System.Text;
using JwtAuth.Models;
using JwtAuth.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuth.Services;

// Resultado de crear un refresh token:
//  - Token:     valor en claro, se entrega UNA vez al cliente.
//  - TokenHash: lo que se guarda en la BD.
public record RefreshTokenResult(string Token, string TokenHash, DateTime ExpiresAt);

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshTokenResult GenerateRefreshToken();
    string HashToken(string token);
}

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _jwt = options.Value;

    public string GenerateAccessToken(User user)
    {
        // 1. La clave: bytes de la SecretKey. La misma se usará para validar.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));

        // 2. Credenciales de firma: clave + algoritmo (HMAC-SHA256).
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Descriptor: todo lo que va dentro del token.
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,       // claim "iss"
            Audience = _jwt.Audience,   // claim "aud"
            Expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes), // claim "exp"
            SigningCredentials = credentials,
            // Claims propios (el payload). Se usan nombres cortos estándar.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),  // identificador del usuario
                [JwtRegisteredClaimNames.Name] = user.Name,
                ["role"] = user.Role.Description,                    // base de la autorización por roles
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString() // id único del token
            }
        };

        // 4. Serializa header.payload y añade la firma -> "xxxxx.yyyyy.zzzzz"
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        // No es un JWT: son 64 bytes aleatorios criptográficamente seguros, sin significado.
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        return new RefreshTokenResult(
            token,
            HashToken(token),
            DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays));
    }

    // SHA-256 en hexadecimal (64 caracteres). Suficiente porque el token ya es aleatorio y largo;
    // para contraseñas, en cambio, se usa un hash lento (PasswordHasher).
    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
