using JwtAuth.Data;
using JwtAuth.Dtos;
using JwtAuth.Models;
using JwtAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// ControllerBase ya tiene una propiedad llamada "User" (el ClaimsPrincipal del token),
// que choca con nuestra entidad User. Un alias evita la ambigüedad.
using AppUser = JwtAuth.Models.User;

namespace JwtAuth.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous] // login y registro son públicos: el JWT es justamente lo que obtienes aquí
public class AuthController(
    AppDbContext db,
    IPasswordHasher<AppUser> hasher,
    ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Name == request.Name))
            return Conflict("Ese nombre de usuario ya existe.");

        // SEGURIDAD: el rol lo decide el servidor, nunca el cliente. Todo registro público es "User".
        var role = await db.Roles.SingleAsync(r => r.Description == "User");

        var user = new AppUser { Name = request.Name, Role = role };
        // El hasher genera una sal aleatoria y usa PBKDF2; la sal queda dentro del propio hash.
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created($"/users/{user.Id}", new { user.Id, user.Name, Role = role.Description });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Include: trae también el Role, porque el token lleva el rol como claim.
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Name == request.Name);

        // Mismo resultado si falla el usuario o la contraseña: no revelamos qué usuarios existen.
        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();

        var response = IssueTokens(user);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        // El cliente envía el token en claro; en la BD solo tenemos hashes, así que buscamos por hash.
        var hash = tokens.HashToken(request.RefreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null)
            return Unauthorized();

        // REUTILIZACIÓN: este token ya fue canjeado (rotado) antes. Quien lo envía ahora es o bien
        // el atacante que lo robó, o bien el usuario legítimo con un token ya robado y usado.
        // No sabemos cuál, así que se cierran TODAS las sesiones del usuario.
        if (stored.RevokedAt is not null && stored.ReplacedByTokenHash is not null)
        {
            var active = await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var t in active)
                t.RevokedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Unauthorized();
        }

        if (!stored.IsActive)   // revocado por logout, o expirado
            return Unauthorized();

        // ROTACIÓN: el token viejo se invalida y se emite un par nuevo.
        var response = IssueTokens(stored.User, replacing: stored);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var hash = tokens.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);

        // El access token en curso sigue siendo válido hasta que expire (el servidor no guarda estado
        // de él), pero sin refresh token ya no se puede renovar.
        if (stored is { IsActive: true })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Siempre 204: no revelamos si el token existía.
        return NoContent();
    }

    // Crea access + refresh token. El refresh se añade al contexto (se guarda con SaveChanges).
    private AuthResponse IssueTokens(AppUser user, RefreshToken? replacing = null)
    {
        var accessToken = tokens.GenerateAccessToken(user);

        var refresh = tokens.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,   // a la BD va el hash...
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refresh.ExpiresAt
        });

        if (replacing is not null)
        {
            replacing.RevokedAt = DateTime.UtcNow;
            replacing.ReplacedByTokenHash = refresh.TokenHash;  // deja rastro de la cadena
        }

        // ...y al cliente va el token en claro.
        return new AuthResponse(accessToken, refresh.Token);
    }
}
