using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteAuth
{
    using Context.Auth;

    /// <summary>
    /// account.password holds a PBKDF2 hash string now (PasswordHasher:
    /// pbkdf2-sha256$iterations$pepper$hash, about 90 characters), not 64 hex digits of SHA-256,
    /// so the column is widened to varchar(255). Existing hashes are untouched; each account is
    /// rehashed at its next login.
    /// </summary>
    /// <remarks>
    /// Nothing to do on SQLite, which does not enforce a varchar's length: the column already
    /// takes the longer value. The migration is here so the two providers' histories match.
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteAuthContext))]
    [Migration("20261001000000_Widen_password_hash")]
    public partial class Widen_password_hash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
