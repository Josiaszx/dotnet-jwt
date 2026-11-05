using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuth.Controllers;

[ApiController]
[Route("me")]
[Authorize] // exige un JWT válido; sin él, el middleware responde 401 y el método ni se ejecuta
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // "User" es el ClaimsPrincipal que el middleware construyó a partir del token.
        return Ok(new
        {
            Id = User.FindFirst("sub")?.Value,
            Name = User.Identity?.Name,          // sale del claim "name" (NameClaimType)
            Role = User.FindFirst("role")?.Value,
            IsUser = User.IsInRole("User"),      // usa el claim "role" (RoleClaimType)
            IsAdmin = User.IsInRole("Admin")
        });
    }
}
