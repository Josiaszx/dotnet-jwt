PRAGMA foreign_keys = ON;

CREATE TABLE Role (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Description TEXT NOT NULL UNIQUE
);

CREATE TABLE User (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Name         TEXT    NOT NULL UNIQUE,
    PasswordHash TEXT    NOT NULL,
    RoleId       INTEGER NOT NULL REFERENCES Role(Id)
);

CREATE TABLE RefreshToken (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    -- Hash del token (nunca el token en claro).
    TokenHash           TEXT    NOT NULL UNIQUE,
    UserId              INTEGER NOT NULL REFERENCES User(Id) ON DELETE CASCADE,
    CreatedAt           DATETIME    NOT NULL,
    ExpiresAt           DATETIME    NOT NULL,
    -- NULL mientras el token siga activo.
    RevokedAt           DATETIME    NULL,
    -- Hash del token que lo reemplazó al rotar.
    ReplacedByTokenHash TEXT    NULL
);

CREATE INDEX IX_User_RoleId         ON User(RoleId);
CREATE INDEX IX_RefreshToken_UserId ON RefreshToken(UserId);

-- Datos iniciales
INSERT INTO Role (Description) VALUES ('Admin'), ('User');
