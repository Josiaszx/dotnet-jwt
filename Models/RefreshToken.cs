using System;
using System.Collections.Generic;

namespace JwtAuth.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public string TokenHash { get; set; } = null!;

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public virtual User User { get; set; } = null!;
}
