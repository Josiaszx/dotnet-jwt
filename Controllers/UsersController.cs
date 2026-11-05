using JwtAuth.Authorization;
using JwtAuth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Controllers;

[ApiController]
[Route("users")]
[Authorize] // regla base: todo el controlador exige estar autenticado
public class UsersController(AppDbContext db) : ControllerBase
{
    // Solo Admin. Un User autenticado recibe 403.
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var users = await db.Users
            .Select(u => new { u.Id, u.Name, Role = u.Role.Description })
            .ToListAsync();

        return Ok(users);
    }

    // El propio usuario o un Admin.
    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.OwnerOrAdmin)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await db.Users
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.Name, Role = u.Role.Description })
            .SingleOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }
}
