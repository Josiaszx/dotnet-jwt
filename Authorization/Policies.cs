using Microsoft.AspNetCore.Authorization;

namespace JwtAuth.Authorization;

public static class Policies
{
    // Constantes: un error de tipeo en "AdminOnly" dentro de un atributo no lo detecta el compilador.
    public const string AdminOnly = "AdminOnly";
    public const string OwnerOrAdmin = "OwnerOrAdmin";

    public static void AddAppPolicies(this AuthorizationOptions options)
    {
        // Política simple: equivale a [Authorize(Roles = "Admin")], pero con nombre y reutilizable.
        options.AddPolicy(AdminOnly, policy => policy.RequireRole("Admin"));

        // Política con lógica propia: un usuario puede ver SU recurso; un Admin puede ver cualquiera.
        options.AddPolicy(OwnerOrAdmin, policy => policy.RequireAssertion(context =>
        {
            if (context.User.IsInRole("Admin"))
                return true;

            // En endpoints, context.Resource es el HttpContext: de ahí leemos el {id} de la ruta.
            var routeId = (context.Resource as HttpContext)?.Request.RouteValues["id"]?.ToString();
            var tokenId = context.User.FindFirst("sub")?.Value;

            return routeId is not null && routeId == tokenId;
        }));
    }
}
