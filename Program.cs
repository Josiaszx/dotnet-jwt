using JwtAuth.Authorization;
using JwtAuth.Data;
using JwtAuth.Models;
using JwtAuth.Options;
using JwtAuth.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Patrón Options: enlaza la sección "JwtSettings" a la clase tipada y la valida al arrancar.
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Autenticación: "quién eres". El esquema Bearer lee el header Authorization: Bearer <token>.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Autorización: "qué puedes hacer" (se aplica con [Authorize]).
builder.Services.AddAuthorization(options => options.AddAppPolicies());

// Las reglas de validación se configuran aparte para poder inyectar JwtSettings (la misma
// configuración con la que TokenService firma). Así emitir y validar nunca se desincronizan.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        // No renombrar claims: que "sub", "name" y "role" se queden como los escribimos en el token.
        bearer.MapInboundClaims = false;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,   // comprueba la FIRMA (lo más importante)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),

            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateLifetime = true,           // rechaza tokens expirados (exp) o aún no válidos (nbf)
            ClockSkew = TimeSpan.Zero,         // por defecto hay 5 min de tolerancia; aquí ninguna

            // Dónde buscar el nombre y el rol dentro del token (los nombres cortos que usamos al crearlo).
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

// El ORDEN importa: primero identificar (autenticación), luego decidir permisos (autorización).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
