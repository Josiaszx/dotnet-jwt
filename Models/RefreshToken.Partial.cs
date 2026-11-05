namespace JwtAuth.Models;

// Extensión manual de la entidad generada por scaffold (que es "partial"): este archivo no se sobrescribe.
public partial class RefreshToken
{
    // Propiedad calculada de solo lectura: EF no la mapea a ninguna columna.
    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
